using Biflow.Ui.Api.Mediator.Queries.Executions;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class ExecutionsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.ExecutionsRead]);
        
        var group = app.MapGroup("/executions")
            .WithTags(Scopes.ExecutionsRead)
            .AddEndpointFilter(endpointFilter);

        group.MapGet("/running",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new RunningExecutionsQuery(limit, lastExecutionId);
                var executions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get currently running executions")
            .WithName("GetRunningExecutions");
        
        group.MapGet("/notstarted",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new NotStartedExecutionsQuery(limit, lastExecutionId);
                var executions = await mediator.SendAsync(query, cancellationToken); 
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get pending/not started executions")
            .WithName("GetNotStartedExecutions");

        group.MapGet("",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] DateTime startDate,
                [FromQuery] DateTime endDate,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new ExecutionsQuery(startDate, endDate, limit, lastExecutionId);
                var executions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get executions")
            .WithName("GetExecutions");
        
        group.MapGet("{executionId:guid}",
            async (IMediator mediator,
                Guid executionId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeParameters = false,
                [FromQuery] bool includeConcurrencies = false,
                [FromQuery] bool includeDataObjects = false) =>
            {
                var query = new ExecutionQuery(
                    executionId,
                    IncludeParameters: includeParameters,
                    IncludeConcurrencies: includeConcurrencies,
                    IncludeDataObjects: includeDataObjects);
                var execution = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(execution);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Execution>()
            .WithDescription("Get execution by id")
            .WithName("GetExecution");
        
        group.MapGet("{executionId:guid}/steps",
            async (IMediator mediator,
                Guid executionId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeAttempts = false,
                [FromQuery] bool includeDependencies = false,
                [FromQuery] bool includeMonitors = false,
                [FromQuery] bool includeDataObjects = false,
                [FromQuery] bool includeParameters = false) =>
            {
                var query = new ExecutionStepsQuery(
                    executionId,
                    includeAttempts,
                    includeDependencies,
                    includeMonitors,
                    includeDataObjects,
                    includeParameters);
                var stepExecutions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<StepExecution[]>()
            .WithDescription("Get execution steps")
            .WithName("GetExecutionSteps");
        
        group.MapGet("{executionId:guid}/steps/{stepId:guid}",
            async (IMediator mediator,
                Guid executionId,
                Guid stepId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeAttempts = false,
                [FromQuery] bool includeDependencies = false,
                [FromQuery] bool includeMonitors = false,
                [FromQuery] bool includeDataObjects = false,
                [FromQuery] bool includeParameters = false) =>
            {
                var query = new StepExecutionQuery(
                    ExecutionId: executionId,
                    StepId: stepId,
                    IncludeAttempts: includeAttempts,
                    IncludeDependencies: includeDependencies,
                    IncludeMonitors: includeMonitors,
                    IncludeDataObjects: includeDataObjects,
                    IncludeParameters: includeParameters);
                var stepExecution = await mediator.SendAsync(query, cancellationToken); 
                return Results.Ok(stepExecution);
            })
            .Produces(StatusCodes.Status404NotFound)
            .Produces<StepExecution>()
            .WithDescription("Get execution step by id")
            .WithName("GetExecutionStep");
        
        group.MapGet("/steps/running",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null,
                [FromQuery] Guid? lastStepId = null,
                [FromQuery] int? lastRetryAttemptIndex = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var page = (lastExecutionId, lastStepId, lastRetryAttemptIndex);
                if (page is not ((null, null, null) or (not null, not null, not null)))
                {
                    return Results.Problem(
                        $"All three query parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(lastRetryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new RunningStepExecutionsQuery(limit, lastExecutionId, lastStepId, lastRetryAttemptIndex);
                var stepExecutions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get all running steps")
            .WithName("GetRunningStepExecutions");
        
        group.MapGet("/steps/notstarted",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null,
                [FromQuery] Guid? lastStepId = null,
                [FromQuery] int? lastRetryAttemptIndex = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var page = (lastExecutionId, lastStepId, lastRetryAttemptIndex);
                if (page is not ((null, null, null) or (not null, not null, not null)))
                {
                    return Results.Problem(
                        $"All three query parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(lastRetryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new NotStartedStepExecutionsQuery(limit, lastExecutionId, lastStepId, lastRetryAttemptIndex);
                var stepExecutions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get all pending/not started step executions")
            .WithName("GetNotStartedStepExecutions");
        
        group.MapGet("/steps",
            async (IMediator mediator,
                CancellationToken cancellationToken,
                [FromQuery] DateTime startDate,
                [FromQuery] DateTime endDate,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null,
                [FromQuery] Guid? lastStepId = null,
                [FromQuery] int? lastRetryAttemptIndex = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var page = (lastExecutionId, lastStepId, lastRetryAttemptIndex);
                if (page is not ((null, null, null) or (not null, not null, not null)))
                {
                    return Results.Problem(
                        $"All three query parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(lastRetryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = new StepExecutionsQuery(startDate, endDate, limit, lastExecutionId, lastStepId, lastRetryAttemptIndex);
                var stepExecutions = await mediator.SendAsync(query, cancellationToken);
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get step executions")
            .WithName("GetStepExecutions");
    }
}