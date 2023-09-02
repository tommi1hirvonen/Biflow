using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

public class JobDuplicatorFactory
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public JobDuplicatorFactory(IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<JobDuplicator> CreateAsync(Guid jobId)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstAsync(j => j.JobId == jobId);
        var steps = await context.Steps
            .IncludeNavigationPropertiesForDuplication()
            .Where(step => step.JobId == jobId)
            .ToArrayAsync();
        var copy = job.Copy();
        job.Steps = steps
            .Select(s => s.Copy(copy))
            .ToList();
        return new JobDuplicator(context, copy);
    }
}
