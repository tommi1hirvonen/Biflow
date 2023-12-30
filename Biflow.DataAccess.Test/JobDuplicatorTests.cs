using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class JobDuplicatorTests(DatabaseFixture fixture)
{
    private readonly JobDuplicatorFactory _jobDuplicatorFactory = fixture.JobDuplicatorFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    [Fact]
    public async Task TestDuplicatingJob()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var job1 = await dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Steps)
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstAsync(j => j.JobName == "Test job 1");
        using var duplicator = await _jobDuplicatorFactory.CreateAsync(job1.JobId);
        duplicator.Job.JobName = "Test job 1 - Copy";
        await duplicator.SaveJobAsync();
        var job1Copy = await dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Steps)
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstAsync(j => j.JobName == "Test job 1 - Copy");
        Assert.Equal(job1.Steps.Count, job1Copy.Steps.Count);
        Assert.NotEmpty(job1Copy.Steps);

        Assert.Equal(job1.JobConcurrencies.Count, job1Copy.JobConcurrencies.Count);
        Assert.NotEmpty(job1Copy.JobConcurrencies);

        Assert.Equal(job1.JobParameters.Count, job1Copy.JobParameters.Count);
        Assert.NotEmpty(job1Copy.JobParameters);

        Assert.Equal(job1.CategoryId, job1Copy.CategoryId);
        Assert.Equal(job1.UseDependencyMode, job1Copy.UseDependencyMode);
        Assert.Equal(job1.StopOnFirstError, job1Copy.StopOnFirstError);
        Assert.Equal(job1.MaxParallelSteps, job1Copy.MaxParallelSteps);
        Assert.Equal(job1.OvertimeNotificationLimitMinutes, job1Copy.OvertimeNotificationLimitMinutes);
    }
}
