namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record NotStartedStepExecutionsQuery(
    int Limit,
    Guid? LastExecutionId,
    Guid? LastStepId,
    int? LastRetryAttemptIndex) : IRequest<StepExecution[]>;

[UsedImplicitly]
internal class NotStartedStepExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<NotStartedStepExecutionsQuery, StepExecution[]>
{
    public async Task<StepExecution[]> Handle(NotStartedStepExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.StepExecutionAttempts
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(e => e.ExecutionId)
            .ThenBy(e => e.StepId)
            .ThenBy(e => e.RetryAttemptIndex)
            .Where(e => e.ExecutionStatus == StepExecutionStatus.NotStarted);
        if (request is { LastExecutionId: { } executionId, LastStepId: { } stepId, LastRetryAttemptIndex: { } retryAttemptIndex })
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