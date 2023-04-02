using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class StepTests : IClassFixture<StepFixture>
{
    private string Username { get; }

    private Step Step { get; }

    public StepTests(StepFixture stepFixture)
    {
        Username = stepFixture.Username;
        Step = stepFixture.Step;
    }

    [Fact] public void CreatedBy_Equals_Username() => Assert.Equal(Username, Step.CreatedBy);

    [Fact] public void LastModifiedBy_Equals_Username() => Assert.Equal(Username, Step.LastModifiedBy);

    [Fact] public void CreatedDateTime_NotEquals_Default() => Assert.NotEqual(default, Step.CreatedDateTime);

    [Fact] public void LastModifiedDateTime_NotEquals_Default() => Assert.NotEqual(default, Step.LastModifiedDateTime);

    [Fact] public void Job_NotNull() => Assert.NotNull(Step.Job);

    [Fact] public void Sources_NotEmpty() => Assert.NotEmpty(Step.Sources);

    [Fact] public void Targets_NotEmpty() => Assert.NotEmpty(Step.Targets);

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

public class StepFixture : IAsyncLifetime
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public StepFixture(DatabaseFixture fixture)
    {
        _dbContextFactory = fixture.DbContextFactory;
        Username = fixture.Username;
    }

    public Step Step { get; private set; } = null!;

    public string Username { get; private set; }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        Step = await context.Steps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .ThenInclude(dep => dep.DependantOnStep)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .FirstAsync(step => step.StepName == "Test step 4");
    }
}