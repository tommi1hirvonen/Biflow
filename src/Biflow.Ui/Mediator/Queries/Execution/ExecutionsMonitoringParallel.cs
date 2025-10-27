using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Biflow.Ui;

/// <summary>
/// Alternative parallel query for <see cref="ExecutionsMonitoringQuery"/>.
/// Can produce unreliable results if the statuses change between the queries.
/// Using separate parallel queries avoids the union (distinct) operator in the translated T-SQL.
/// </summary>
/// <param name="FromDateTime"></param>
/// <param name="ToDateTime"></param>
public record ExecutionsMonitoringParallelQuery(DateTime FromDateTime, DateTime ToDateTime)
    : IRequest<ExecutionsMonitoringQueryResponse>;


[UsedImplicitly]
internal class ExecutionsMonitoringParallelQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ExecutionsMonitoringParallelQuery, ExecutionsMonitoringQueryResponse>
{
    public async Task<ExecutionsMonitoringQueryResponse> Handle(ExecutionsMonitoringParallelQuery request, CancellationToken cancellationToken)
    {
        // Generate list of query predicates.
        // The predicates are logically designed so that they do not produce duplicates

        var predicates = Enumerable.Empty<Expression<Func<Execution, bool>>>()
            .Append(e => e.CreatedOn <= request.ToDateTime && e.EndedOn >= request.FromDateTime);

        if (DateTime.Now >= request.FromDateTime && DateTime.Now <= request.ToDateTime)
        {
            predicates = predicates
                // Adds executions that were not started.
                .Append(e => e.CreatedOn >= request.FromDateTime
                        && e.CreatedOn <= request.ToDateTime
                        && e.EndedOn == null
                        && e.ExecutionStatus != ExecutionStatus.Running)
                // Adds currently running executions if current time fits in the time window.
                .Append(e => e.ExecutionStatus == ExecutionStatus.Running);
        }
        else
        {
            predicates = predicates
                // Adds executions that were not started and executions that may still be running.
                .Append(e => e.CreatedOn >= request.FromDateTime && e.CreatedOn <= request.ToDateTime && e.EndedOn == null);
        }

        // Map the query predicates to each become its own query to fetch executions.
        var tasks = predicates.Select(p => GetExecutionsAsync(p, cancellationToken));

        // Combine and flatten query results.
        var executions = await Task.WhenAll(tasks);
        var flatten = executions
            .SelectMany(e => e)
            .OrderByDescending(e => e.CreatedOn)
            .ThenByDescending(e => e.StartedOn)
            .ToArray();
        return new ExecutionsMonitoringQueryResponse(flatten);
    }

    private async Task<ExecutionProjection[]> GetExecutionsAsync(
        Expression<Func<Execution, bool>> predicate, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Executions
            .AsNoTracking()
            .AsSingleQuery()
            .Where(predicate);
        return await (
            from e in query
            join job in context.Jobs on e.JobId equals job.JobId into ej
            from job in ej.DefaultIfEmpty() // Translates to left join in SQL
            select new ExecutionProjection(
                e.ExecutionId,
                e.JobId,
                job.JobName ?? e.JobName,
                e.ScheduleId,
                e.ScheduleName,
                e.CreatedBy,
                e.CreatedOn,
                e.StartedOn,
                e.EndedOn,
                e.ExecutionStatus,
                e.StepExecutions.Count(),
                job.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray()
            )).ToArrayWithNoLockAsync(cancellationToken);
    }
}