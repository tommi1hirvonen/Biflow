using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.Exceptions;
using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.Projections;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        services.AddSingleton(typeof(ITokenService), typeof(TokenService<ExecutorDbContext>));
        services.AddOptions<ExecutionOptions>()
            .Bind(executorConfiguration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<EmailOptions>(executorConfiguration);

        services.AddSingleton<IExecutionValidator, CircularJobsValidator>();
        services.AddSingleton<IExecutionValidator, CircularStepsValidator>();
        services.AddSingleton<IExecutionValidator, HybridModeValidator>();

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
        services.AddHostedService(services => services.GetRequiredService<IExecutionManager>());
        // Timeout for hosted services (e.g. ExecutionManager) to shut down gracefully when StopAsync() is called.
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));

        // Add step executors
        services.Scan(selector =>
            selector.FromAssemblyOf<IStepExecutor>()
                .AddClasses(filter => filter.AssignableTo(typeof(IStepExecutor<,>)))
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

        return services;
    }

    public static WebApplication MapExecutorEndpoints(this WebApplication app)
    {
        app.MapGet("/execution/start/{executionId}", async (Guid executionId, IExecutionManager executionManager) =>
        {
            try
            {
                await executionManager.StartExecutionAsync(executionId);
                return Results.Ok();
            }
            catch (DuplicateExecutionException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).WithName("StartExecution");


        app.MapGet("/execution/stop/{executionId}", (Guid executionId, string username, IExecutionManager executionManager) =>
        {
            try
            {
                executionManager.CancelExecution(executionId, username);
                return Results.Ok();
            }
            catch (ExecutionNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).WithName("StopExecution");


        app.MapGet("/execution/stop/{executionId}/{stepId}", (Guid executionId, Guid stepId, string username, IExecutionManager executionManager) =>
        {
            try
            {
                executionManager.CancelExecution(executionId, username, stepId);
                return Results.Ok();
            }
            catch (ExecutionNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).WithName("StopExecutionStep");


        app.MapGet("/execution/status/{executionId}", (Guid executionId, IExecutionManager executionManager) =>
        {
            return executionManager.IsExecutionRunning(executionId)
                ? Results.Ok()
                : Results.NotFound();
        }).WithName("ExecutionStatus");


        app.MapGet("/execution/status", (bool? includeSteps, IExecutionManager executionManager) =>
        {
            var executions = executionManager.CurrentExecutions
                .Select(e =>
                 {
                     var steps = includeSteps == true
                         ? e.StepExecutions.Select(s =>
                         {
                             var attempts = s.StepExecutionAttempts
                                 .Select(a => new StepExecutionAttemptProjection(a.RetryAttemptIndex, a.StartedOn, a.EndedOn, a.ExecutionStatus))
                                 .ToArray();
                             return new StepExecutionProjection(s.StepId, s.StepName, s.StepType, s.ExecutionStatus, attempts);
                         })
                         .ToArray()
                         : null;
                     return new ExecutionProjection(e.ExecutionId, e.JobId, e.JobName, e.ExecutionMode, e.CreatedOn, e.StartedOn, e.ExecutionStatus, steps);
                 }).ToArray();
            return executions;
        }).WithName("ExecutionsStatus");


        app.MapGet("/connection/test", async (IConnectionTest connectionTest) =>
        {
            await connectionTest.RunAsync();
        }).WithName("TestConnection");


        app.MapPost("/email/test", async (string address, IEmailTest emailTest) =>
        {
            await emailTest.RunAsync(address);
            return Results.Ok("Email test succeeded. Check that the email was received successfully.");
        }).WithName("TestEmail");

        return app;
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
        var matches = replacementRules.Where(rule => input.Contains(rule.Key));
        if (!matches.Any())
        {
            return input;
        }

        var match = matches.First();
        int startIndex = input.IndexOf(match.Key);
        int endIndex = startIndex + match.Key.Length;

        var before = input[..startIndex].Replace(replacementRules);
        var replaced = match.Value;
        var after = input[endIndex..].Replace(replacementRules);

        return before + replaced + after;
    }

    /// <summary>
    /// Maps step execution parameters to a Dictionary
    /// </summary>
    /// <param name="parameters">Step execution parameters to map</param>
    /// <returns>Dictionary where the parameter name is the key and the parameter value is the value</returns>
    internal static Dictionary<string, string?> ToStringDictionary(this IEnumerable<StepExecutionParameterBase> parameters)
    {
        return parameters.Select(p => p.ParameterValue switch
        {
            DateTime dt => (Name: p.ParameterName, Value: dt.ToString("o")),
            _ => (Name: p.ParameterName, Value: p.ParameterValue?.ToString())
        })
        .ToDictionary(key => key.Name, value => value.Value);
    }

    internal static IEnumerable<T> SelectNotNull<T, U>(this IEnumerable<U> source, Func<U, T?> selector)
        where T : class
    {
        return source.Select(selector).Where(t => t is not null).Cast<T>();
    }
}
