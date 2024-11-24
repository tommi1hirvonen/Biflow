using Biflow.Ui.Core.Projection;
using System.Linq.Expressions;

namespace Biflow.Ui.Core;

/// <summary>
/// Alternative parallel query for <see cref="StepExecutionsMonitoringQuery"/>.
/// Can produce unreliable results if the statuses change between the queries.
/// Using separate parallel queries avoids the union (distinct) operator in the translated T-SQL.
/// </summary>
/// <param name="FromDateTime"></param>
/// <param name="ToDateTime"></param>
public record StepExecutionsMonitoringParallelQuery(DateTime FromDateTime, DateTime ToDateTime)
    : IRequest<StepExecutionsMonitoringQueryResponse>;

internal class StepExecutionsParallelQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<StepExecutionsMonitoringParallelQuery, StepExecutionsMonitoringQueryResponse>
{
    public async Task<StepExecutionsMonitoringQueryResponse> Handle(StepExecutionsMonitoringParallelQuery request, CancellationToken cancellationToken)
    {
        // Generate list of query predicates.
        // The predicates are logically designed so that they do not produce duplicates

        var predicates = Enumerable.Empty<Expression<Func<StepExecutionAttempt, bool>>>()
            .Append(e => e.StepExecution.Execution.CreatedOn <= request.ToDateTime && e.EndedOn >= request.FromDateTime);

        if (DateTime.Now >= request.FromDateTime && DateTime.Now <= request.ToDateTime)
        {
            predicates = predicates
                // Adds executions that were not started.
                .Append(e => e.StepExecution.Execution.CreatedOn >= request.FromDateTime
                    && e.StepExecution.Execution.CreatedOn <= request.ToDateTime
                    && e.EndedOn == null
                    && e.ExecutionStatus != StepExecutionStatus.Running)
                // Adds currently running executions if current time fits in the time window.
                .Append(e => e.ExecutionStatus == StepExecutionStatus.Running);
        }
        else
        {
            predicates = predicates
                // Adds executions that were not started and executions that may still be running.
                .Append(e => e.StepExecution.Execution.CreatedOn >= request.FromDateTime
                    && e.StepExecution.Execution.CreatedOn <= request.ToDateTime
                    && e.EndedOn == null);
        }

        // Map the query predicates to each become its own query to fetch executions.
        var tasks = predicates.Select(p => GetExecutionsAsync(p, cancellationToken));

        // Combine and flatten query results.
        var executions = await Task.WhenAll(tasks);
        var flatten = executions
            .SelectMany(e => e)
            .OrderByDescending(e => e.CreatedOn)
            .ThenByDescending(e => e.StartedOn)
            .ThenByDescending(e => e.ExecutionPhase)
            .ToArray();

        return new StepExecutionsMonitoringQueryResponse(flatten);
    }

    private async Task<StepExecutionProjection[]> GetExecutionsAsync(
        Expression<Func<StepExecutionAttempt, bool>> predicate, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.StepExecutionAttempts
            .AsNoTracking()
            .AsSingleQuery()
            .Where(predicate);
        return await (
            from e in query
            join job in context.Jobs on e.StepExecution.Execution.JobId equals job.JobId into j
            from job in j.DefaultIfEmpty()
            join step in context.Steps on e.StepId equals step.StepId into s
            from step in s.DefaultIfEmpty()
            select new StepExecutionProjection(
                e.StepExecution.ExecutionId,
                e.StepExecution.StepId,
                e.RetryAttemptIndex,
                step.StepName ?? e.StepExecution.StepName,
                e.StepType,
                e.StepExecution.ExecutionPhase,
                e.StepExecution.Execution.CreatedOn,
                e.StartedOn,
                e.EndedOn,
                e.ExecutionStatus,
                e.StepExecution.Execution.ExecutionStatus,
                e.StepExecution.Execution.ExecutionMode,
                e.StepExecution.Execution.ScheduleId,
                e.StepExecution.Execution.ScheduleName,
                e.StepExecution.Execution.JobId,
                job.JobName ?? e.StepExecution.Execution.JobName,
                step.Dependencies.Select(d => d.DependantOnStepId).ToArray(),
                step.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray(),
                job.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray()
            )).ToArrayAsync(cancellationToken);
    }
}