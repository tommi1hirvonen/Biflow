using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class JobTests
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly string _username;

    public JobTests(DatabaseFixture fixture)
    {
        _dbContextFactory = fixture.DbContextFactory;
        _username = fixture.Username;
    }

    [Fact]
    public async Task TestJob()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTracking()
            .Include(job => job.Category)
            .FirstAsync(job => job.JobName == "Test job");
        Assert.Equal(_username, job.CreatedBy);
        Assert.Equal(_username, job.LastModifiedBy);
        Assert.NotEqual(default, job.CreatedDateTime);
        Assert.NotEqual(default, job.LastModifiedDateTime);
        Assert.NotNull(job.Category);
    }
}
