namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record RunningExecutionsQuery(int Limit, Guid? LastExecutionId) : IRequest<Execution[]>;

[UsedImplicitly]
internal class RunningExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<RunningExecutionsQuery, Execution[]>
{
    public async Task<Execution[]> Handle(RunningExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = dbContext.Executions
            .AsNoTracking()
            .Where(e => e.ExecutionStatus == ExecutionStatus.Running)
            .OrderBy(e => e.ExecutionId)
            .AsQueryable();
        
        if (request.LastExecutionId is { } id)
        {
            query = query.Where(e => e.ExecutionId > id);
        }
        
        var executions = await query
            .Take(request.Limit)
            .ToArrayWithNoLockAsync(cancellationToken);
        
        return executions;
    }
}