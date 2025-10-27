namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record StepExecutionsQuery(
    DateTime StartDate,
    DateTime EndDate,
    int Limit,
    Guid? LastExecutionId,
    Guid? LastStepId,
    int? LastRetryAttemptIndex) : IRequest<StepExecution[]>;

[UsedImplicitly]
internal class StepExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<StepExecutionsQuery, StepExecution[]>
{
    public async Task<StepExecution[]> Handle(StepExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.StepExecutionAttempts
            .AsNoTrackingWithIdentityResolution()
            .Where(e => e.StartedOn <= request.EndDate && e.EndedOn >= request.StartDate)
            .OrderBy(e => e.ExecutionId)
            .ThenBy(e => e.StepId)
            .ThenBy(e => e.RetryAttemptIndex)
            .AsQueryable();
        if (request is
            { LastExecutionId: { } executionId, LastStepId: { } stepId, LastRetryAttemptIndex: { } retryAttemptIndex })
        {
            query = query
                .Where(e => e.ExecutionId > executionId 
                            || (e.ExecutionId == executionId && e.StepId > stepId)
                            || (e.ExecutionId == executionId && e.StepId == stepId && e.RetryAttemptIndex > retryAttemptIndex));
        }
        var stepExecutionAttempts = await query
            .Include(e => e.StepExecution)
            .Take(request.Limit)
            .ToArrayWithNoLockAsync(cancellationToken);
        var stepExecutions = stepExecutionAttempts
            .Select(e => e.StepExecution)
            .OrderBy(e => e.ExecutionId)
            .ThenBy(e => e.StepId)
            .ToArray();
        return stepExecutions;
    }
}