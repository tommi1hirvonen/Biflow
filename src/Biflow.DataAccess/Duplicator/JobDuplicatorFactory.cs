namespace Biflow.DataAccess;

public class JobDuplicatorFactory(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<JobDuplicator> CreateAsync(Guid jobId)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<Job> query = context.Jobs
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .Include(j => j.Tags);
        query = DuplicatorExtensions.StepNavigationPaths
            .Skip(1)
            .Aggregate(query, (current, include) => current.Include($"{nameof(Job.Steps)}.{include}"));
        var job = await query.FirstAsync(j => j.JobId == jobId);
        var copy = job.Copy();
        return new JobDuplicator(context, copy);
    }
}
