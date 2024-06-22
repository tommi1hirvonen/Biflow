using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class StepsDuplicatorTests(DatabaseFixture fixture)
{
    private readonly StepsDuplicatorFactory _stepsDuplicatorFactory = fixture.StepsDuplicatorFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    [Fact]
    public async Task StepsDuplicatorProducesExpected()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var step = (SqlStep)await dbContext.Steps
            .AsNoTracking()
            .Include(s => (s as SqlStep)!.StepParameters)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ThenInclude(p => p.JobParameter)
            .FirstAsync(s => s.StepName == "Test step 4" && s.Job.JobName == "Test job 1");
        using var duplicator = await _stepsDuplicatorFactory.CreateAsync(step.StepId);
        foreach (var s in duplicator.Steps)
        {
            s.StepName += " - Copy";
        }
        await duplicator.SaveStepsAsync();
        var stepCopy = (SqlStep)await dbContext.Steps
            .AsNoTracking()
            .Include(s => (s as SqlStep)!.StepParameters)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ThenInclude(p => p.JobParameter)
            .FirstAsync(s => s.StepName == "Test step 4 - Copy" && s.Job.JobName == "Test job 1");

        Assert.Equal(step.Tags.Count, stepCopy.Tags.Count);
        Assert.NotEmpty(stepCopy.Tags);

        Assert.Equal(step.Dependencies.Count, stepCopy.Dependencies.Count);
        Assert.NotEmpty(stepCopy.Dependencies);

        Assert.Equal(step.DataObjects.Count, stepCopy.DataObjects.Count);
        Assert.NotEmpty(stepCopy.DataObjects);

        Assert.Equal(step.ExecutionConditionParameters.Count, stepCopy.ExecutionConditionParameters.Count);
        Assert.NotEmpty(stepCopy.ExecutionConditionParameters);

        Assert.Equal(step.SqlStatement, stepCopy.SqlStatement);
        Assert.Equal(step.ExecutionPhase, stepCopy.ExecutionPhase);
        Assert.Equal(step.StepDescription, stepCopy.StepDescription);
        Assert.Equal(step.ExecutionConditionExpression.Expression, stepCopy.ExecutionConditionExpression.Expression);
    }
}
