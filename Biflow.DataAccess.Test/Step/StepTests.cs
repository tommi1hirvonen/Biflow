using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class StepTests(StepFixture stepFixture) : IClassFixture<StepFixture>
{
    private string Username { get; } = stepFixture.Username;

    private Step Step { get; } = stepFixture.Step;

    [Fact] public void CreatedBy_Equals_Username() => Assert.Equal(Username, Step.CreatedBy);

    [Fact] public void LastModifiedBy_Equals_Username() => Assert.Equal(Username, Step.LastModifiedBy);

    [Fact] public void CreatedDateTime_NotEquals_Default() => Assert.NotEqual(default, Step.CreatedOn);

    [Fact] public void LastModifiedDateTime_NotEquals_Default() => Assert.NotEqual(default, Step.LastModifiedOn);

    [Fact] public void Job_NotNull() => Assert.NotNull(Step.Job);

    [Fact] public void DataObjects_NotEmpty() => Assert.NotEmpty(Step.DataObjects);

    [Fact] public void Dependencies_NotEmpty() => Assert.NotEmpty(Step.Dependencies);

    [Fact] public void Tags_NotEmpty() => Assert.NotEmpty(Step.Tags);

    [Fact]
    public void GreaterExecutionPhase_ComparesTo_Greater()
    {
        var phase20 = new SqlStep { StepName = "Step 1", ExecutionPhase = 20 };
        var phase10 = new SqlStep { StepName = "Step 2", ExecutionPhase = 10 };
        Assert.True(phase20.CompareTo(phase10) > 0);
    }

    [Fact]
    public void SameExecutionPhase_DefaultsTo_NameComparison()
    {
        var stepName1 = new SqlStep { StepName = "Step 1", ExecutionPhase = 10 };
        var stepName2 = new SqlStep { StepName = "Step 2", ExecutionPhase = 10 };
        Assert.True(stepName2.CompareTo(stepName1) > 0);
    }
}

public class StepFixture(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    public Step Step { get; private set; } = null!;

    public string Username { get; private set; } = fixture.Username;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        Step = await context.Steps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .ThenInclude(dep => dep.DependantOnStep)
            .Include(step => step.DataObjects)
            .FirstAsync(step => step.StepName == "Test step 4");
    }
}