namespace Biflow.Ui;

public record StepExecutionsMonitoringQuery(DateTime FromDateTime, DateTime ToDateTime)
    : IRequest<StepExecutionsMonitoringQueryResponse>;

public record StepExecutionsMonitoringQueryResponse(IEnumerable<StepExecutionProjection> Executions);

internal class StepExecutionsQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<StepExecutionsMonitoringQuery, StepExecutionsMonitoringQueryResponse>
{
    public async Task<StepExecutionsMonitoringQueryResponse> Handle(StepExecutionsMonitoringQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var from = request.FromDateTime;
        var to = request.ToDateTime;

        var query = context.StepExecutionAttempts
            .AsNoTracking()
            .Where(e => e.StepExecution.Execution.CreatedOn <= to && e.EndedOn >= from);

        if (DateTime.Now >= from && DateTime.Now <= to)
        {
            query = query
                // Adds executions that were not started.
                .Union(context.StepExecutionAttempts
                    .Where(e => e.StepExecution.Execution.CreatedOn >= from
                            && e.StepExecution.Execution.CreatedOn <= to
                            && e.EndedOn == null
                            && e.ExecutionStatus != StepExecutionStatus.Running))
                // Adds currently running executions if current time fits in the time window.
                .Union(context.StepExecutionAttempts
                    .Where(e => e.ExecutionStatus == StepExecutionStatus.Running));
        }
        else
        {
            query = query
                // Adds executions that were not started and executions that may still be running.
                .Union(context.StepExecutionAttempts
                    .Where(e => e.StepExecution.Execution.CreatedOn >= from
                            && e.StepExecution.Execution.CreatedOn <= to
                            && e.EndedOn == null));
        }

        var executions = await (
            from e in query
            join job in context.Jobs on e.StepExecution.Execution.JobId equals job.JobId into j
            from job in j.DefaultIfEmpty()
            join step in context.Steps on e.StepId equals step.StepId into s
            from step in s.DefaultIfEmpty()
            orderby e.StepExecution.Execution.CreatedOn descending, e.StartedOn descending, e.StepExecution.ExecutionPhase descending
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
                job.JobName ?? e.StepExecution.Execution.JobName,
                step.Dependencies.Select(d => d.DependantOnStepId).ToArray(),
                step.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray(),
                job.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray()
            )).ToArrayAsync(cancellationToken);

        return new StepExecutionsMonitoringQueryResponse(executions);
    }
}
