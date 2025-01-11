namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record NotStartedExecutionsQuery(int Limit, Guid? LastExecutionId) : IRequest<Execution[]>;

[UsedImplicitly]
internal class NotStartedExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<NotStartedExecutionsQuery, Execution[]>
{
    public async Task<Execution[]> Handle(NotStartedExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = dbContext.Executions
            .AsNoTracking()
            .Where(e => e.ExecutionStatus != ExecutionStatus.Running && e.EndedOn == null)
            .OrderBy(e => e.ExecutionId)
            .AsQueryable();
        
        if (request.LastExecutionId is { } id)
        {
            query = query.Where(e => e.ExecutionId > id);
        }
        
        var executions = await query
            .Take(request.Limit)
            .ToArrayAsync(cancellationToken);
        
        return executions;
    }
}