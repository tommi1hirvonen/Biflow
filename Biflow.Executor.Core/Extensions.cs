using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.JobOrchestrator;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.ServiceHealth;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biflow.Executor.Core;

public static class Extensions
{
    public static IServiceCollection AddExecutorServices(this IServiceCollection services, IConfiguration executorConfiguration)
    {
        services.AddDbContextFactory<ExecutorDbContext>();
        services.AddExecutionBuilderFactory<ExecutorDbContext>();
        services.AddHttpClient();
        services.AddHttpClient("notimeout", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddMemoryCache();
        services.AddSingleton(typeof(ITokenService), typeof(TokenService<ExecutorDbContext>));
        services.AddOptions<ExecutionOptions>()
            .Bind(executorConfiguration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<EmailOptions>(executorConfiguration);

        services.AddSingleton<IExecutionValidator, CircularJobsValidator>();
        services.AddSingleton<IExecutionValidator, CircularStepsValidator>();
        services.AddSingleton<IExecutionValidator, HybridModeValidator>();

        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.JobExecutorHealthService);
        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.NotificationHealthService);
        services.AddSingleton<ISubscriptionsProviderFactory, SubscriptionsProviderFactory>();
        services.AddSingleton<ISubscribersResolver, SubscribersResolver>();
        services.AddSingleton<IMessageDispatcher, EmailDispatcher>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IStepExecutorProvider, StepExecutorProvider>();
        services.AddSingleton<IGlobalOrchestrator, GlobalOrchestrator>();
        services.AddSingleton<IJobOrchestratorFactory, JobOrchestratorFactory>();
        services.AddSingleton<IEmailTest, EmailTest>();
        services.AddSingleton<IConnectionTest, ConnectionTest.ConnectionTest>();
        services.AddSingleton<IJobExecutorFactory, JobExecutorFactory>();
        services.AddSingleton<IExecutionManager, ExecutionManager>();
        services.AddHostedService(s => s.GetRequiredService<IExecutionManager>());
        // Timeout for hosted services (e.g. ExecutionManager) to shut down gracefully when StopAsync() is called.
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));

        // Scan assembly and add step executors as their implemented type and as singletons.
        var stepExecutorType = typeof(IStepExecutor<,>);
        var types = stepExecutorType.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IStepExecutor)));
        foreach (var type in types)
        {
            var @interface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == stepExecutorType);
            if (@interface is null) continue;
            services.AddSingleton(@interface, type);
        }
        
        services.AddHealthChecks()
            .AddCheck<JobExecutorHealthCheck>("job_executor", tags: ["executor"])
            .AddCheck<NotificationHealthCheck>("notification_service", tags: ["executor"]);

        return services;
    }

    /// <summary>
    /// Replace sections of a string based on multiple rules.
    /// The method makes sure not to replace the same section twice.
    /// </summary>
    /// <param name="input">The string to which the replacement rules are applied</param>
    /// <param name="replacementRules">Replacement rules where the key is the substring to search for and the value is the replacement.</param>
    /// <returns></returns>
    internal static string Replace(this string input, IDictionary<string, string?> replacementRules)
    {
        var matches = replacementRules
            .Where(rule => input.Contains(rule.Key))
            .ToArray();
        
        if (matches.Length == 0)
        {
            return input;
        }

        var (searchValue, replacement) = matches.First();
        
        var startIndex = input.IndexOf(searchValue, StringComparison.Ordinal);
        var endIndex = startIndex + searchValue.Length;

        var before = input[..startIndex].Replace(replacementRules);
        var after = input[endIndex..].Replace(replacementRules);

        return before + replacement + after;
    }

    /// <summary>
    /// Maps step execution parameters to a Dictionary
    /// </summary>
    /// <param name="parameters">Step execution parameters to map</param>
    /// <returns>Dictionary where the parameter name is the key and the parameter value is the value</returns>
    internal static Dictionary<string, string?> ToStringDictionary(this IEnumerable<StepExecutionParameterBase> parameters)
    {
        return parameters.Select(p => p.ParameterValue.Value switch
        {
            DateTime dt => (Name: p.ParameterName, Value: dt.ToString("o")),
            _ => (Name: p.ParameterName, Value: p.ParameterValue.Value?.ToString())
        })
        .ToDictionary(key => key.Name, value => value.Value);
    }
}