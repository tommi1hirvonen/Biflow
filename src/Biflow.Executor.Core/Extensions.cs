using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.JobOrchestrator;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Notification.Options;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.ServiceHealth;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biflow.Executor.Core;

public static class Extensions
{
    public static IServiceCollection AddExecutorServices(this IServiceCollection services,
        IConfiguration executorConfiguration)
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

        services.AddSingleton<IExecutionValidator, CircularJobsValidator>();
        services.AddSingleton<IExecutionValidator, CircularStepsValidator>();
        services.AddSingleton<IExecutionValidator, HybridModeValidator>();

        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.JobExecutorHealthService);
        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.NotificationHealthService);
        services.AddSingleton<ISubscriptionsProviderFactory, SubscriptionsProviderFactory>();
        services.AddSingleton<ISubscribersResolver, SubscribersResolver>();
        services.AddSingleton<INotificationMessageService, NotificationMessageService>();

        // Configure email options and services based on which section was configured in appsettings.
        var emailSection = executorConfiguration.GetSection(EmailOptions.Section);
        var smtpSection = emailSection.GetSection(SmtpOptions.Section);
        var azureEmailSection = emailSection.GetSection(AzureEmailOptions.Section);
        var graphEmailSection = emailSection.GetSection(GraphEmailOptions.Section);
        if (smtpSection.Exists())
        {
            services.Configure<SmtpOptions>(smtpSection);
            services.AddSingleton<IMessageDispatcher, SmtpDispatcher>();
            services.AddSingleton<IEmailTest, SmtpTest>();
        }
        else if (azureEmailSection.Exists())
        {
            services.Configure<AzureEmailOptions>(azureEmailSection);
            services.AddSingleton<IMessageDispatcher, AzureEmailDispatcher>();
            services.AddSingleton<IEmailTest, AzureEmailTest>();
        }
        else if (graphEmailSection.Exists())
        {
            services.Configure<GraphEmailOptions>(graphEmailSection);
            services.AddSingleton<IMessageDispatcher, GraphEmailDispatcher>();
            services.AddSingleton<IEmailTest, GraphEmailTest>();
        }

        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IStepOrchestrator, StepOrchestrator>();
        services.AddSingleton<IStepExecutorProvider, StepExecutorProvider>();
        services.AddSingleton<IGlobalOrchestrator, GlobalOrchestrator>();
        services.AddSingleton<IJobOrchestratorFactory, JobOrchestratorFactory>();
        services.AddSingleton<IJobExecutorFactory, JobExecutorFactory>();
        services.AddSingleton<IExecutionManager, ExecutionManager>();
        services.AddHostedService(s => s.GetRequiredService<IExecutionManager>());
        // Timeout for hosted services (e.g., ExecutionManager) to shut down gracefully when StopAsync() is called.
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));
        
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

    /// <summary>
    /// Executes an asynchronous operation with a retry mechanism that supports late cancellation.
    /// The first attempt will ignore the cancellation token, while subsequent retries will respect it.
    /// </summary>
    /// <param name="action">The asynchronous operation to execute, which accepts a <see cref="CancellationToken"/>.</param>
    /// <param name="retryCount">The number of retries to attempt if the action fails and meets the retry condition.</param>
    /// <param name="sleepDurationProvider">A function that determines the delay duration before each retry based on the retry attempt index.</param>
    /// <param name="shouldRetry">A predicate that evaluates whether the exception thrown by the action qualifies for a retry.</param>
    /// <param name="cancellationToken">The cancellation token used to signal cancellation for retries and delay intervals.</param>
    /// <returns>A task that represents the asynchronous operation with retries.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the retry logic encounters an internal failure or reaches an invalid state.</exception>
    public static Task ExecuteWithLateCancellationRetryAsync(
        Func<CancellationToken, Task> action,
        int retryCount,
        Func<int, TimeSpan> sleepDurationProvider,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken) =>
        // Do retries recursively, starting with the first attempt (index 0).
        ExecuteWithLateCancellationRetryInternalAsync(
            action,
            retryCount,
            attempt: 0,
            sleepDurationProvider,
            shouldRetry,
            cancellationToken);

    private static async Task ExecuteWithLateCancellationRetryInternalAsync(
        Func<CancellationToken, Task> action,
        int retryCount,
        int attempt,
        Func<int, TimeSpan> sleepDurationProvider,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken)
    {
        try
        {
            // The first attempt ignores the cancellation token (== late cancellation).
            var effectiveToken = attempt == 0 ? CancellationToken.None : cancellationToken;
            await action(effectiveToken);
        }
        catch (Exception ex) when (attempt < retryCount && shouldRetry(ex))
        {
            // If cancellation was requested, throw the original exception.
            if (cancellationToken.IsCancellationRequested)
                throw;
        
            var delay = sleepDurationProvider(attempt + 1);
            await Task.Delay(delay, cancellationToken);
        
            await ExecuteWithLateCancellationRetryInternalAsync(
                action,
                retryCount,
                attempt + 1,
                sleepDurationProvider,
                shouldRetry,
                cancellationToken);
        }
    }
}