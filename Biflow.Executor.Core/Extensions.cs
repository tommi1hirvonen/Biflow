using Biflow.Executor.Core.Authentication;
using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.Exceptions;
using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.FilesExplorer;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.JobOrchestrator;
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

        return services;
    }

    public static WebApplication MapExecutorEndpoints(this WebApplication app)
    {
        var executionsGroup = app
            .MapGroup("/executions")
            .WithName("Executions")
            .AddEndpointFilter<ServiceApiKeyEndpointFilter>();


        executionsGroup.MapPost("/create", async (
            ExecutionCreateRequest request,
            IExecutionBuilderFactory<ExecutorDbContext> executionBuilderFactory,
            IExecutionManager executionManager) =>
        {
            using var builder = await executionBuilderFactory.CreateAsync(
                request.JobId,
                createdBy: "API",
                predicates:
                [
                    _ => step => (request.StepIds == null && step.IsEnabled)
                                 || (request.StepIds != null && request.StepIds.Contains(step.StepId))
                ]);
            if (builder is null)
            {
                return Results.NotFound("Job not found");
            }
            builder.AddAll();
            var execution = await builder.SaveExecutionAsync();
            if (execution is null)
            {
                return Results.Conflict("Execution contained no steps");
            }
            if (request.StartExecution == true)
            {
                await executionManager.StartExecutionAsync(execution.ExecutionId);
            }
            return Results.Json(new ExecutionCreateResponse(execution.ExecutionId), statusCode: 201);
        }).WithName("CreateExecution");


        executionsGroup.MapGet("/start/{executionId:guid}", async (Guid executionId, IExecutionManager executionManager, CancellationToken cancellationToken) =>
        {
            try
            {
                await executionManager.StartExecutionAsync(executionId, cancellationToken);
                return Results.Ok();
            }
            catch (DuplicateExecutionException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).WithName("StartExecution");


        executionsGroup.MapGet("/stop/{executionId:guid}", (Guid executionId, string username, IExecutionManager executionManager) =>
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


        executionsGroup.MapGet("/stop/{executionId:guid}/{stepId:guid}", (Guid executionId, Guid stepId, string username, IExecutionManager executionManager) =>
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


        executionsGroup.MapGet("/status/{executionId:guid}", (Guid executionId, IExecutionManager executionManager) =>
            executionManager.IsExecutionRunning(executionId) 
                ? Results.Ok() 
                : Results.NotFound()).WithName("ExecutionStatus");


        executionsGroup.MapGet("/status", (bool? includeSteps, IExecutionManager executionManager) =>
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
        
        
        app.MapGet("/tokencache/clear/{azureCredentialId:guid}",
                async (Guid azureCredentialId, ITokenService tokenService, CancellationToken cancellationToken) =>
        {
            await tokenService.ClearAsync(azureCredentialId, cancellationToken);
        }).WithName("ClearTokenCache")
        .AddEndpointFilter<ServiceApiKeyEndpointFilter>();


        app.MapGet("/connection/test", async (IConnectionTest connectionTest) =>
        {
            await connectionTest.RunAsync();
        })
        .WithName("TestConnection")
        .AddEndpointFilter<ServiceApiKeyEndpointFilter>();


        app.MapPost("/email/test", async (string address, IEmailTest emailTest) =>
        {
            await emailTest.RunAsync(address);
            return Results.Ok("Email test succeeded. Check that the email was received successfully.");
        })
        .WithName("TestEmail")
        .AddEndpointFilter<ServiceApiKeyEndpointFilter>();
        
        app.MapPost("/fileexplorer/search", (FileExplorerSearchRequest request) =>
        {
            var items = FileExplorer.GetDirectoryItems(request.Path);
            var response = new FileExplorerSearchResponse(items);
            return Results.Ok(response);
        })
        .WithName("FileExplorerSearch")
        .AddEndpointFilter<ServiceApiKeyEndpointFilter>();

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

[PublicAPI]
file record ExecutionCreateRequest(Guid JobId, Guid[]? StepIds, bool? StartExecution);

[PublicAPI]
file record ExecutionCreateResponse(Guid ExecutionId);

[PublicAPI]
public record FileExplorerSearchRequest(string? Path);

public record FileExplorerSearchResponse(DirectoryItem[] Items);