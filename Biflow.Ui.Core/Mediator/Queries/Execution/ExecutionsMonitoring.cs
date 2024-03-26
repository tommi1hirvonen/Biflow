using Biflow.Ui.Core.Projection;

namespace Biflow.Ui.Core;

public record ExecutionsMonitoringQuery(DateTime FromDateTime, DateTime ToDateTime)
    : IRequest<ExecutionsMonitoringQueryResponse>;

public record ExecutionsMonitoringQueryResponse(IEnumerable<ExecutionProjection> Executions);

internal class ExecutionsQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ExecutionsMonitoringQuery, ExecutionsMonitoringQueryResponse>
{
    public async Task<ExecutionsMonitoringQueryResponse> Handle(ExecutionsMonitoringQuery request, CancellationToken cancellationToken)
    {
        using var context = dbContextFactory.CreateDbContext();
        var from = request.FromDateTime;
        var to = request.ToDateTime;

        var query = context.Executions
                .AsNoTracking()
                .AsSingleQuery()
                // Index optimized way of querying executions without having to scan the entire table.
                .Where(e => e.CreatedOn <= to && e.EndedOn >= from);

        if (DateTime.Now >= from && DateTime.Now <= to)
        {
            query = query
                .Union(context.Executions
                    // Adds executions that were not started.
                    .Where(e => e.CreatedOn >= from
                            && e.CreatedOn <= to
                            && e.EndedOn == null
                            && e.ExecutionStatus != ExecutionStatus.Running))
                .Union(context.Executions
                    // Adds currently running executions if current time fits in the time window.
                    .Where(e => e.ExecutionStatus == ExecutionStatus.Running));
        }
        else
        {
            query = query
                .Union(context.Executions
                    // Adds executions that were not started and executions that may still be running.
                    .Where(e => e.CreatedOn >= from
                            && e.CreatedOn <= to
                            && e.EndedOn == null));
        }

        var executions = await (
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

        return new ExecutionsMonitoringQueryResponse(executions);
    }
}