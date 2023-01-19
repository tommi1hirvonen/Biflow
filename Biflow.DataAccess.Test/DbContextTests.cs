using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess.Test;

public class DbContextTests
{
    private IDbContextFactory<BiflowContext> dbContextFactory;

    public DbContextTests()
    {
        var settings = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var connectionString = "Data Source=localhost;Database=Biflow;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddDbContextFactory<BiflowContext>(options =>
                    options.UseSqlServer(connectionString, o =>
                        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)))
            .BuildServiceProvider();

        var factory = services.GetService<IDbContextFactory<BiflowContext>>();
        Assert.NotNull(factory);
        dbContextFactory = factory;
    }

    [Fact]
    public async Task TestLoadingSteps()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        var steps = await context.Steps.Include("StepParameters").ToListAsync();
        
        var parametersExist = steps.Any(s => s is IHasStepParameters p && p.StepParameters.Any());
        Assert.True(parametersExist);

        var sqlParamsExist = steps.Any(s => s is IHasStepParameters<SqlStepParameter> p && p.StepParameters.Any());
        Assert.True(sqlParamsExist);

        var packageParamsExist = steps.Any(s => s is IHasStepParameters<PackageStepParameter> p && p.StepParameters.Any());
        Assert.True(packageParamsExist);
    }

    [Fact]
    public async Task TestLoadingStepExecutions()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        var stepExecutions = await context.StepExecutions.Include("StepExecutionParameters").ToListAsync();
        
        var parametersExist = stepExecutions.Any(s => s is IHasStepExecutionParameters p && p.StepExecutionParameters.Any());
        Assert.True(parametersExist);

        var sqlParamsExist = stepExecutions.Any(s => s is IHasStepExecutionParameters<SqlStepExecutionParameter> p && p.StepExecutionParameters.Any());
        Assert.True(sqlParamsExist);

        var packageParamsExist = stepExecutions.Any(s => s is IHasStepExecutionParameters<PackageStepExecutionParameter> p && p.StepExecutionParameters.Any());
        Assert.True(packageParamsExist);
    }

    [Fact]
    public async Task TestLoadingExecutions()
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
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

        var packageParamsExist = executions
            .SelectMany(e => e.StepExecutions)
            .Any(s => s is IHasStepExecutionParameters<PackageStepExecutionParameter> p && p.StepExecutionParameters.Any());
        Assert.True(packageParamsExist);

        var inherit = executions
            .SelectMany(e => e.StepExecutions)
            .Any(s => s is IHasStepExecutionParameters p && p.StepExecutionParameters.Any(ep => ep.InheritFromExecutionParameter is not null));
        Assert.True(inherit);
    }
}