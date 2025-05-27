using Biflow.Executor.Core.Exceptions;
using Biflow.Executor.Core.Models;
using Biflow.Executor.Core.Notification;
using Biflow.ExecutorProxy.Core.Authentication;
using Biflow.ExecutorProxy.Core.FilesExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Biflow.Executor.Core;

public static class Endpoints
{
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
        })
        .Produces<ExecutionCreateResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .WithSummary("Create and optionally start a new execution of the given job")
        .WithDescription("Create a new execution of the given job. " +
                         "Optionally the execution can be immediately started with the startExecution property " +
                         "in the request body.")
        .WithName("CreateExecution");


        executionsGroup.MapGet("/start/{executionId:guid}", async (
            Guid executionId, IExecutionManager executionManager, CancellationToken cancellationToken) =>
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
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status409Conflict)
        .WithSummary("Start execution")
        .WithDescription("Start an execution that has already been created in the database")
        .WithName("StartExecution");


        executionsGroup.MapGet("/stop/{executionId:guid}", (
            Guid executionId, string username, IExecutionManager executionManager) =>
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
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Stop job execution")
        .WithDescription("Stop an entire execution with the given execution id")
        .WithName("StopExecution");


        executionsGroup.MapGet("/stop/{executionId:guid}/{stepId:guid}", (
            Guid executionId, Guid stepId, string username, IExecutionManager executionManager) =>
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
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithSummary("Stop step execution")
        .WithDescription("Stop a step in a job execution")
        .WithName("StopExecutionStep");


        executionsGroup.MapGet("/status/{executionId:guid}", (Guid executionId, IExecutionManager executionManager) =>
            executionManager.IsExecutionRunning(executionId) 
                ? Results.Ok() 
                : Results.NotFound())
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Get whether an execution is running and registered with the executor service")
            .WithDescription("Get whether an execution is running and registered with the executor service. " +
                             "If the execution is being managed by the executor service, status 200 (OK) will be returned. " +
                             "If the execution is not being managed by the service, status 404 (Not Found) will be returned.")
            .WithName("ExecutionStatus");


        executionsGroup.MapGet("/status", (bool? includeSteps, IExecutionManager executionManager) =>
        {
            var executions = executionManager.CurrentExecutions
                .Select(e =>
                 {
                     var steps = includeSteps == true
                         ? e.StepExecutions.Select(s =>
                         {
                             var attempts = s.StepExecutionAttempts
                                 .Select(a => new StepExecutionAttemptProjection(
                                     a.RetryAttemptIndex, a.StartedOn, a.EndedOn, a.ExecutionStatus))
                                 .ToArray();
                             return new StepExecutionProjection(
                                 s.StepId, s.StepName, s.StepType, s.ExecutionStatus, attempts);
                         })
                         .ToArray()
                         : null;
                     return new ExecutionProjection(
                         e.ExecutionId,
                         e.JobId,
                         e.JobName,
                         e.ExecutionMode,
                         e.CreatedOn,
                         e.StartedOn,
                         e.ExecutionStatus,
                         steps);
                 }).ToArray();
            return executions;
        })
        .Produces<ExecutionProjection[]>()
        .WithSummary("Get a list of executions currently being managed by the executor service")
        .WithDescription("Get a list of executions currently being managed by the executor service. " +
                         "Optionally include steps for all executions in the response.")
        .WithName("ExecutionsStatus");
        
        
        app.MapGet("/tokencache/clear/{azureCredentialId:guid}",
            async (Guid azureCredentialId, ITokenService tokenService, CancellationToken cancellationToken) =>
            {
                await tokenService.ClearAsync(azureCredentialId, cancellationToken);
            })
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Clear Azure credential token cache")
            .WithDescription("Clear Azure credential token cache. Tokens are cached in memory for 5 minutes.")
            .WithName("ClearTokenCache")
            .AddEndpointFilter<ServiceApiKeyEndpointFilter>();


        app.MapPost("/email/test", async (string address, IEmailTest emailTest) => 
            {
                await emailTest.RunAsync(address);
                return Results.Ok("Email test succeeded. Check that the email was received successfully.");
            })
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Send a test email")
            .WithDescription("Send a test email using the email settings from configuration")
            .WithName("TestEmail")
            .AddEndpointFilter<ServiceApiKeyEndpointFilter>();
        
        
        app.MapPost("/fileexplorer/search", (FileExplorerSearchRequest request) =>
            {
                var items = FileExplorer.GetDirectoryItems(request.Path);
                var response = new FileExplorerSearchResponse(items);
                return Results.Ok(response);
            })
            .Produces<FileExplorerSearchResponse>()
            .WithSummary("Search for local files on the executor service machine")
            .WithDescription("Search for local files on the executor service machine")
            .WithName("FileExplorerSearch")
            .AddEndpointFilter<ServiceApiKeyEndpointFilter>();

        return app;
    }
}