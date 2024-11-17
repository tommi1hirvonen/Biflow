namespace Biflow.DataAccess;

public class StepsDuplicatorFactory(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public Task<StepsDuplicator> CreateAsync(Guid stepId, Guid? targetJobId = null) =>
        CreateAsync([stepId], targetJobId);

    public async Task<StepsDuplicator> CreateAsync(Guid[] stepIds, Guid? targetJobId = null)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Steps.Where(s => stepIds.Contains(s.StepId));
        query = DuplicatorExtensions.StepNavigationPaths
            .Aggregate(query, (current, include) => current.Include(include));
        var steps = await query.ToArrayAsync();
        var targetJob = targetJobId is { } id
            ? await context.Jobs.Include(j => j.JobParameters).FirstOrDefaultAsync(j => j.JobId == id)
            : null;
        var copies = steps.Select(s => s.Copy(targetJob)).ToList();
        return new StepsDuplicator(context, copies);
    }
}
