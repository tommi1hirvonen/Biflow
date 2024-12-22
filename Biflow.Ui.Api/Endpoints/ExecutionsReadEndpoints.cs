using Biflow.Core.Constants;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    return Results.BadRequest("Limit must be between 10 and 100");
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
                        return Results.BadRequest("Limit must be between 10 and 100");
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
                    return Results.BadRequest("Limit must be between 10 and 100");
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
            .WithDescription("Get executions")
            .WithName("GetExecutions");
        
        group.MapGet("{executionId:guid}",
            async (ServiceDbContext dbContext,
                Guid executionId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeParameters = false,
                [FromQuery] bool includeConcurrencies = false,
                [FromQuery] bool includeDataObjects = false,
                [FromQuery] bool includeSteps = false) =>
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
                    return Results.NotFound();
                }
                if (!includeSteps)
                {
                    return Results.Ok(execution);
                }
                var stepExecutions = await dbContext.StepExecutions
                    .AsNoTrackingWithIdentityResolution()
                    .Where(e => e.ExecutionId == executionId)
                    .Include(e => e.StepExecutionAttempts)
                    .Include(e => e.ExecutionDependencies)
                    .Include(e => e.MonitoringStepExecutions)
                    .Include(e => e.MonitoredStepExecutions)
                    .Include(e => e.DataObjects)
                    .Include(
                        $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                    .Include(e => e.ExecutionConditionParameters)
                    .ToArrayAsync(cancellationToken);
                foreach (var stepExecution in stepExecutions)
                {
                    execution.StepExecutions.Add(stepExecution);
                }
                return Results.Ok(execution);
            })
            .WithDescription("Get execution by id")
            .WithName("GetExecution");
    }
}