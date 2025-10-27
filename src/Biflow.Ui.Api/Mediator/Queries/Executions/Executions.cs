namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record ExecutionsQuery(DateTime StartDate, DateTime EndDate, int Limit, Guid? LastExecutionId)
    : IRequest<Execution[]>;

[UsedImplicitly]
internal class ExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<ExecutionsQuery, Execution[]>
{
    public async Task<Execution[]> Handle(ExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = dbContext.Executions
            .AsNoTracking()
            .AsSingleQuery()
            // Index optimized way of querying executions without having to scan the entire table.
            .Where(e => e.CreatedOn <= request.EndDate && e.EndedOn >= request.StartDate)
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