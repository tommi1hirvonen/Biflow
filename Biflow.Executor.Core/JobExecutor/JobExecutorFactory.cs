using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutorFactory(IServiceProvider serviceProvider, IDbContextFactory<ExecutorDbContext> dbContextFactory) : IJobExecutorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    public async Task<IJobExecutor> CreateAsync(Guid executionId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.Job)
            .Include(e => e.ExecutionParameters)
            .Include(e => e.ExecutionConcurrencies)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.StepExecutionAttempts)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.ExecutionDependencies)
            .ThenInclude(e => e.DependantOnStepExecution)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.ExecutionConditionParameters)
            .ThenInclude(e => e.ExecutionParameter)
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
            .Include(e => e.StepExecutions)
            .ThenInclude(e => (e as DatasetStepExecution)!.AppRegistration)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => (e as FunctionStepExecution)!.FunctionApp)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => (e as QlikStepExecution)!.QlikCloudClient)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => (e as PipelineStepExecution)!.PipelineClient)
            .ThenInclude(df => df.AppRegistration)
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasConnection.Connection)}")
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId);
        return execution is null
            ? throw new ArgumentException($"No execution was found for id {executionId}")
            : ActivatorUtilities.CreateInstance<JobExecutor>(_serviceProvider, execution);
    }
}
