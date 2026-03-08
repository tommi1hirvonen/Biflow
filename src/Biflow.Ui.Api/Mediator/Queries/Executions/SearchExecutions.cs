namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record SearchExecutionsQuery(
    Guid[]? JobIds,
    Guid[]? ScheduleIds,
    ExecutionStatus[]? ExecutionStatuses,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int Limit,
    DateTimeOffset? LastCreatedOn,
    Guid? LastExecutionId,
    bool IncludeParameters)
    : IRequest<Execution[]>;

[UsedImplicitly]
internal class SearchExecutionsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<SearchExecutionsQuery, Execution[]>
{
    public async Task<Execution[]> Handle(SearchExecutionsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Executions
            .AsNoTracking()
            .AsSingleQuery()
            .AsQueryable();

        if (request.IncludeParameters)
        {
            query = query.Include(e => e.ExecutionParameters);
        }

        if (request.JobIds is { Length: > 0 })
        {
            query = query.Where(e => request.JobIds.Contains(e.JobId));
        }

        if (request.ScheduleIds is { Length: > 0 })
        {
            query = query.Where(e => e.ScheduleId != null && request.ScheduleIds.Contains(e.ScheduleId.Value));
        }

        if (request.ExecutionStatuses is { Length: > 0 })
        {
            query = query.Where(e => request.ExecutionStatuses.Contains(e.ExecutionStatus));
        }

        if (request.StartDate is { } startDate)
        {
            query = query.Where(e => e.CreatedOn >= startDate);
        }

        if (request.EndDate is { } endDate)
        {
            query = query.Where(e => e.CreatedOn <= endDate);
        }

        query = query
            .OrderByDescending(e => e.CreatedOn)
            .ThenByDescending(e => e.ExecutionId);

        if (request.LastCreatedOn is { } lastCreatedOn && request.LastExecutionId is { } lastExecutionId)
        {
            query = query.Where(e => e.CreatedOn < lastCreatedOn ||
                                     (e.CreatedOn == lastCreatedOn && e.ExecutionId.CompareTo(lastExecutionId) < 0));
        }

        var executions = await query
            .Take(request.Limit)
            .ToArrayWithNoLockAsync(cancellationToken);

        return executions;
    }
}
