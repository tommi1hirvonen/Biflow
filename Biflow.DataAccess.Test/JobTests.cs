using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class JobTests(JobFixture fixture) : IClassFixture<JobFixture>
{
    private readonly string _username = fixture.Username;

    private Job Job { get; } = fixture.Job;

    [Fact] public void CreatedBy_Equals_Username() => Assert.Equal(_username, Job.CreatedBy);

    [Fact] public void LastModifiedBy_Equals_Username() => Assert.Equal(_username, Job.LastModifiedBy);

    [Fact] public void CreatedDateTime_NotEquals_Default() => Assert.NotEqual(default, Job.CreatedDateTime);

    [Fact] public void LastModifiedDateTime_NotEquals_Default() => Assert.NotEqual(default, Job.LastModifiedDateTime);

    [Fact] public void Category_NotNull() => Assert.NotNull(Job.Category);
}

public class JobFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    public Job Job { get; private set; } = null!;

    public string Username { get; } = fixture.Username;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        Job = await context.Jobs
            .Include(job => job.Category)
            .FirstAsync(job => job.JobName == "Test job");
    }
}