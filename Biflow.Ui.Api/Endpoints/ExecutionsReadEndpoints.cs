using Biflow.Core.Interfaces;
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
            async (ServiceDbContext dbContext,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = dbContext.Executions
                    .AsNoTracking()
                    .Where(e => e.ExecutionStatus == ExecutionStatus.Running)
                    .OrderBy(e => e.ExecutionId)
                    .AsQueryable();
                if (lastExecutionId is { } id)
                {
                    query = query.Where(e => e.ExecutionId > id);
                }
                var executions = await query
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get currently running executions")
            .WithName("GetRunningExecutions");
        
        group.MapGet("/notstarted",
            async (ServiceDbContext dbContext,
                CancellationToken cancellationToken,
                [FromQuery] int limit = 100,
                [FromQuery] Guid? lastExecutionId = null) =>
            {
                if (limit is < 10 or > 100)
                {
                    return Results.Problem("Limit must be between 10 and 100",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var query = dbContext.Executions
                    .AsNoTracking()
                    .Where(e => e.ExecutionStatus != ExecutionStatus.Running && e.EndedOn == null)
                    .OrderBy(e => e.ExecutionId)
                    .AsQueryable();
                if (lastExecutionId is { } id)
                {
                    query = query.Where(e => e.ExecutionId > id);
                }
                var executions = await query
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get pending/not started executions")
            .WithName("GetNotStartedExecutions");

        group.MapGet("",
            async (ServiceDbContext dbContext,
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
                var query = dbContext.Executions
                    .AsNoTracking()
                    .AsSingleQuery()
                    // Index optimized way of querying executions without having to scan the entire table.
                    .Where(e => e.CreatedOn <= endDate && e.EndedOn >= startDate)
                    .OrderBy(e => e.ExecutionId)
                    .AsQueryable();
                if (lastExecutionId is { } id)
                {
                    query = query.Where(e => e.ExecutionId > id);
                }
                var executions = await query
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                return Results.Ok(executions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<Execution[]>()
            .WithDescription("Get executions")
            .WithName("GetExecutions");
        
        group.MapGet("{executionId:guid}",
            async (ServiceDbContext dbContext,
                Guid executionId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeParameters = false,
                [FromQuery] bool includeConcurrencies = false,
                [FromQuery] bool includeDataObjects = false) =>
            {
                var query = dbContext.Executions
                    .AsNoTrackingWithIdentityResolution()
                    .AsQueryable();
                if (includeParameters)
                {
                    query = query.Include(e => e.ExecutionParameters);
                }
                if (includeConcurrencies)
                {
                    query = query.Include(e => e.ExecutionConcurrencies);
                }
                if (includeDataObjects)
                {
                    query = query.Include(e => e.DataObjects);
                }
                var execution = await query
                    .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken);
                if (execution is null)
                {
                    throw new NotFoundException<Execution>(executionId);
                }
                return Results.Ok(execution);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Execution>()
            .WithDescription("Get execution by id")
            .WithName("GetExecution");
        
        group.MapGet("{executionId:guid}/steps",
            async (ServiceDbContext dbContext,
                Guid executionId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeAttempts = false,
                [FromQuery] bool includeDependencies = false,
                [FromQuery] bool includeMonitors = false,
                [FromQuery] bool includeDataObjects = false,
                [FromQuery] bool includeParameters = false) =>
            {
                var executionExists = await dbContext.Executions
                    .AnyAsync(e => e.ExecutionId == executionId, cancellationToken);
                if (!executionExists)
                {
                    throw new NotFoundException<Execution>(executionId);
                }
                var query = dbContext.StepExecutions
                    .AsNoTrackingWithIdentityResolution()
                    .Where(e => e.ExecutionId == executionId);
                if (includeAttempts)
                {
                    query = query.Include(e => e.StepExecutionAttempts);
                }
                if (includeDependencies)
                {
                    query = query.Include(e => e.ExecutionDependencies);
                }
                if (includeMonitors)
                {
                    query = query
                        .Include(e => e.MonitoringStepExecutions)
                        .Include(e => e.MonitoredStepExecutions);
                }
                if (includeDataObjects)
                {
                    query = query.Include(e => e.DataObjects);
                }
                if (includeParameters)
                {
                    query = query
                        .Include(
                            $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                        .Include(e => e.ExecutionConditionParameters);
                }
                var stepExecutions = await query.ToArrayAsync(cancellationToken);
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<StepExecution[]>()
            .WithDescription("Get execution steps")
            .WithName("GetExecutionSteps");
        
        group.MapGet("{executionId:guid}/steps/{stepId:guid}",
            async (ServiceDbContext dbContext,
                Guid executionId,
                Guid stepId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeAttempts = false,
                [FromQuery] bool includeDependencies = false,
                [FromQuery] bool includeMonitors = false,
                [FromQuery] bool includeDataObjects = false,
                [FromQuery] bool includeParameters = false) =>
            {
                var query = dbContext.StepExecutions
                    .AsNoTrackingWithIdentityResolution();
                if (includeAttempts)
                {
                    query = query.Include(e => e.StepExecutionAttempts);
                }
                if (includeDependencies)
                {
                    query = query.Include(e => e.ExecutionDependencies);
                }
                if (includeMonitors)
                {
                    query = query
                        .Include(e => e.MonitoringStepExecutions)
                        .Include(e => e.MonitoredStepExecutions);
                }
                if (includeDataObjects)
                {
                    query = query.Include(e => e.DataObjects);
                }
                if (includeParameters)
                {
                    query = query
                        .Include(
                            $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                        .Include(e => e.ExecutionConditionParameters);
                }
                var stepExecution = await query
                    .FirstOrDefaultAsync(e => e.ExecutionId == executionId && e.StepId == stepId,
                        cancellationToken);
                if (stepExecution is null)
                {
                    throw new NotFoundException<StepExecution>(
                        (nameof(StepExecution.ExecutionId), executionId),
                        (nameof(StepExecution.StepId), stepId));
                }
                return Results.Ok(stepExecution);
            })
            .Produces(StatusCodes.Status404NotFound)
            .Produces<StepExecution>()
            .WithDescription("Get execution step by id")
            .WithName("GetExecutionStep");
        
        group.MapGet("/steps/running",
            async (ServiceDbContext dbContext,
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
                var query = dbContext.StepExecutionAttempts
                    .AsNoTrackingWithIdentityResolution()
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ThenBy(e => e.RetryAttemptIndex)
                    .Where(e => e.ExecutionStatus == StepExecutionStatus.Running);
                if (lastExecutionId is { } executionId
                    && lastStepId is { } stepId
                    && lastRetryAttemptIndex is { } retryAttemptIndex)
                {
                    query = query
                        .Where(e => e.ExecutionId > executionId 
                                    || (e.ExecutionId == executionId && e.StepId > stepId)
                                    || (e.ExecutionId == executionId && e.StepId == stepId && e.RetryAttemptIndex > retryAttemptIndex));
                }
                else if (lastExecutionId is not null || lastStepId is not null || lastRetryAttemptIndex is not null)
                {
                    return Results.Problem(
                        $"All three parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(retryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var stepExecutionAttempts = await query
                    .Include(e => e.StepExecution)
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                var stepExecutions = stepExecutionAttempts
                    .Select(e => e.StepExecution)
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ToArray();
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get all running steps")
            .WithName("GetRunningStepExecutions");
        
        group.MapGet("/steps/notstarted",
            async (ServiceDbContext dbContext,
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
                var query = dbContext.StepExecutionAttempts
                    .AsNoTrackingWithIdentityResolution()
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ThenBy(e => e.RetryAttemptIndex)
                    .Where(e => e.ExecutionStatus == StepExecutionStatus.NotStarted);
                if (lastExecutionId is { } executionId
                    && lastStepId is { } stepId
                    && lastRetryAttemptIndex is { } retryAttemptIndex)
                {
                    query = query
                        .Where(e => e.ExecutionId > executionId 
                                    || (e.ExecutionId == executionId && e.StepId > stepId)
                                    || (e.ExecutionId == executionId && e.StepId == stepId && e.RetryAttemptIndex > retryAttemptIndex));
                }
                else if (lastExecutionId is not null || lastStepId is not null || lastRetryAttemptIndex is not null)
                {
                    return Results.Problem(
                        $"All three parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(retryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var stepExecutionAttempts = await query
                    .Include(e => e.StepExecution)
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                var stepExecutions = stepExecutionAttempts
                    .Select(e => e.StepExecution)
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ToArray();
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get all pending/not started step executions")
            .WithName("GetNotStartedStepExecutions");
        
        group.MapGet("/steps",
            async (ServiceDbContext dbContext,
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
                var query = dbContext.StepExecutionAttempts
                    .AsNoTrackingWithIdentityResolution()
                    .Where(e => e.StartedOn <= endDate && e.EndedOn >= startDate)
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ThenBy(e => e.RetryAttemptIndex)
                    .AsQueryable();
                if (lastExecutionId is { } executionId
                    && lastStepId is { } stepId
                    && lastRetryAttemptIndex is { } retryAttemptIndex)
                {
                    query = query
                        .Where(e => e.ExecutionId > executionId 
                                    || (e.ExecutionId == executionId && e.StepId > stepId)
                                    || (e.ExecutionId == executionId && e.StepId == stepId && e.RetryAttemptIndex > retryAttemptIndex));
                }
                else if (lastExecutionId is not null || lastStepId is not null || lastRetryAttemptIndex is not null)
                {
                    return Results.Problem(
                        $"All three parameters {nameof(lastExecutionId)}, {nameof(lastStepId)} and {nameof(retryAttemptIndex)} " +
                        "must be provided together or all of them must be omitted.",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                var stepExecutionAttempts = await query
                    .Include(e => e.StepExecution)
                    .Take(limit)
                    .ToArrayAsync(cancellationToken);
                var stepExecutions = stepExecutionAttempts
                    .Select(e => e.StepExecution)
                    .OrderBy(e => e.ExecutionId)
                    .ThenBy(e => e.StepId)
                    .ToArray();
                return Results.Ok(stepExecutions);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces<StepExecution[]>()
            .WithDescription("Get step executions")
            .WithName("GetStepExecutions");
    }
}