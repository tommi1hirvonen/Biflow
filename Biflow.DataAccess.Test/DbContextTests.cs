using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biflow.DataAccess.Test;

[Collection(nameof(DatabaseCollection))]
public class DbContextTests(DatabaseFixture fixture)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = fixture.DbContextFactory;

    [Fact]
    public async Task TestLoadingSteps()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var steps = await context.Steps.Include("StepParameters").ToListAsync();
        
        var parametersExist = steps.Any(s => s is IHasStepParameters p && p.StepParameters.Any());
        Assert.True(parametersExist);

        var sqlParamsExist = steps.Any(s => s is IHasStepParameters<SqlStepParameter> p && p.StepParameters.Any());
        Assert.True(sqlParamsExist);
    }

    [Fact]
    public async Task TestLoadingStepExecutions()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var stepExecutions = await context.StepExecutions.Include("StepExecutionParameters").ToListAsync();
        
        var parametersExist = stepExecutions.Any(s => s is IHasStepExecutionParameters p && p.StepExecutionParameters.Any());
        Assert.True(parametersExist);

        var sqlParamsExist = stepExecutions.Any(s => s is IHasStepExecutionParameters<SqlStepExecutionParameter> p && p.StepExecutionParameters.Any());
        Assert.True(sqlParamsExist);
    }

    [Fact]
    public async Task TestLoadingExecutions()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var executions = await context.Executions
            .Include(e => e.StepExecutions)
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}").ToListAsync();

        var parametersExist = executions
            .SelectMany(e => e.StepExecutions)
            .Any(s => s is IHasStepExecutionParameters p && p.StepExecutionParameters.Any());
        Assert.True(parametersExist);

        var sqlParamsExist = executions
            .SelectMany(e => e.StepExecutions)
            .Any(s => s is IHasStepExecutionParameters<SqlStepExecutionParameter> p && p.StepExecutionParameters.Any());
        Assert.True(sqlParamsExist);

        var inherit = executions
            .SelectMany(e => e.StepExecutions)
            .Any(s => s is IHasStepExecutionParameters p && p.StepExecutionParameters.Any(ep => ep.InheritFromExecutionParameter is not null));
        Assert.True(inherit);
    }

    [Fact]
    public async Task TestStepExecutionParameterExpression()
    {
        SqlStepExecution execution;
        await using (var ctx1 = await _dbContextFactory.CreateDbContextAsync())
        {
            execution = (SqlStepExecution)await ctx1.StepExecutions
                .Include(e => e.Execution)
                .Include(e => (e as SqlStepExecution)!.StepExecutionParameters)
                .ThenInclude(p => p.InheritFromExecutionParameter)
                .Include(e => (e as SqlStepExecution)!.StepExecutionParameters)
                .ThenInclude(p => p.ExpressionParameters)
                .ThenInclude(p => p.InheritFromExecutionParameter)
                .Include(e => e.StepExecutionAttempts)
                .FirstAsync(e => e.Execution.JobName == "Test job 1" && e.StepName == "Test step 4");
        }
        var parameter = execution.StepExecutionParameters.First(p => p.ParameterName == "@param");
        var value = await parameter.EvaluateAsync();
        Assert.Equal(value, "123-456");
    }

    [Fact]
    public async Task TestStepExecutionInheritedParameterExpression()
    {
        SqlStepExecution execution;
        await using (var ctx1 = await _dbContextFactory.CreateDbContextAsync())
        {
            execution = (SqlStepExecution)await ctx1.StepExecutions
                .Include(e => e.Execution)
                .Include(e => (e as SqlStepExecution)!.StepExecutionParameters)
                .ThenInclude(p => p.InheritFromExecutionParameter)
                .FirstAsync(e => e.Execution.JobName == "Test job 1" && e.StepName == "Test step 5");
        }
        var parameter = execution.StepExecutionParameters.First(p => p.ParameterName == "@param");
        var value = await parameter.InheritFromExecutionParameter!.EvaluateAsync();
        Assert.Equal(value, "123-456");
    }

    [Fact]
    public async Task TestLoadingStepExecutionDependentOnSteps()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.ExecutionDependencies)
            .FirstAsync(e => e.JobName == "Test job 1");
        var step = execution.StepExecutions.First(s => s.StepName == "Test step 4");
        Assert.True(step.ExecutionDependencies.Count != 0);
    }

    [Fact]
    public async Task GetExecutionWithEntireGraphNotInclude()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var executionId = await context.Executions
            .Where(e => e.JobName == "Test job 1")
            .Select(e => e.ExecutionId)
            .FirstAsync();
        var execution = await context.GetExecutionWithEntireGraphAsync(executionId, includeEndpoint: false, includeStep: false);
        Assert.NotNull(execution);
        Assert.Contains(execution.StepExecutions, x => x is SqlStepExecution sql && sql.GetConnection() is null);
        Assert.Contains(execution.StepExecutions, x => x.GetStep() is null);
    }

    [Fact]
    public async Task GetExecutionWithEntireGraphInclude1()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var executionId = await context.Executions
            .Where(e => e.JobName == "Test job 1")
            .Select(e => e.ExecutionId)
            .FirstAsync();
        var execution = await context.GetExecutionWithEntireGraphAsync(executionId, includeEndpoint: true, includeStep: true);
        Assert.NotNull(execution);
        Assert.Contains(execution.StepExecutions, x => x is SqlStepExecution sql && sql.GetConnection() is not null);
        Assert.Contains(execution.StepExecutions, x => x.GetStep() is not null);
    }

    [Fact]
    public async Task GetExecutionWithEntireGraphInclude2()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var executionId = await context.Executions
            .Where(e => e.JobName == "Test job 2")
            .Select(e => e.ExecutionId)
            .FirstAsync();
        var execution = await context.GetExecutionWithEntireGraphAsync(executionId, includeEndpoint: true, includeStep: true);
        Assert.NotNull(execution);
        Assert.Contains(execution.StepExecutions, x => x.GetStep() is not null);
        Assert.Contains(execution.StepExecutions, x => x is ExeStepExecution exe && exe.GetRunAsCredential() is not null);
    }
}