namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record ExecutionQuery(
    Guid ExecutionId,
    bool IncludeParameters,
    bool IncludeConcurrencies,
    bool IncludeDataObjects) : IRequest<Execution>;

[UsedImplicitly]
internal class ExecutionQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<ExecutionQuery, Execution>
{
    public async Task<Execution> Handle(ExecutionQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.Executions
            .AsNoTrackingWithIdentityResolution()
            .AsQueryable();
        if (request.IncludeParameters)
        {
            query = query.Include(e => e.ExecutionParameters);
        }
        if (request.IncludeConcurrencies)
        {
            query = query.Include(e => e.ExecutionConcurrencies);
        }
        if (request.IncludeDataObjects)
        {
            query = query.Include(e => e.DataObjects);
        }
        var execution = await query
            .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId, cancellationToken);
        return execution ?? throw new NotFoundException<Execution>(request.ExecutionId);
    }
}