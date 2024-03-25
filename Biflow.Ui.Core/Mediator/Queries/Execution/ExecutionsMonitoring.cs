using Biflow.Ui.Core.Projection;
using System.Linq.Expressions;

namespace Biflow.Ui.Core;

public record ExecutionsMonitoringQuery(DateTime FromDateTime, DateTime ToDateTime)
    : IRequest<ExecutionsMonitoringQueryResponse>;

public record ExecutionsMonitoringQueryResponse(IEnumerable<ExecutionProjection> Executions);

internal class ExecutionsQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ExecutionsMonitoringQuery, ExecutionsMonitoringQueryResponse>
{
    public async Task<ExecutionsMonitoringQueryResponse> Handle(ExecutionsMonitoringQuery request, CancellationToken cancellationToken)
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
        var flatten = executions.SelectMany(e => e).ToArray();
        return new ExecutionsMonitoringQueryResponse(flatten);
    }

    private async Task<ExecutionProjection[]> GetExecutionsAsync(
        Expression<Func<Execution, bool>> predicate, CancellationToken cancellationToken)
    {
        using var context = dbContextFactory.CreateDbContext();
        var query = context.Executions
            .AsNoTracking()
            .AsSingleQuery()
            .Where(predicate);
        return await (
            from e in query
            join job in context.Jobs on e.JobId equals job.JobId into ej
            from job in ej.DefaultIfEmpty() // Translates to left join in SQL
            orderby e.CreatedOn descending, e.StartedOn descending
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
                job.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color)).ToArray()
            )).ToArrayAsync(cancellationToken);
    }
}