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
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId)
            ?? throw new ArgumentException($"No execution was found for id {executionId}");

        Task[] mappingTasks = [
            MapAppRegistrationsAsync(execution, context),
            MapFunctionAppsAsync(execution, context),
            MapQlikCloudClientsAsync(execution, context),
            MapPipelineClientsAsync(execution, context),
            MapConnectionsAsync(execution, context)
            ];

        await Task.WhenAll(mappingTasks);

        return ActivatorUtilities.CreateInstance<JobExecutor>(_serviceProvider, execution);
    }

    private static async Task MapAppRegistrationsAsync(Execution execution, ExecutorDbContext context)
    {
        var steps = execution.StepExecutions.OfType<DatasetStepExecution>().ToArray();
        if (steps.Length == 0)
        {
            return;
        }
        var ids = steps.Select(e => e.AppRegistrationId).Distinct().ToArray();
        var items = await context.AppRegistrations.Where(x => ids.Contains(x.AppRegistrationId)).ToArrayAsync();
        var matches = steps.Join(items, s => s.AppRegistrationId, x => x.AppRegistrationId, (s, x) => (s, x));
        foreach (var (step, item) in matches)
        {
            step.AppRegistration = item;
        }
    }

    private static async Task MapFunctionAppsAsync(Execution execution, ExecutorDbContext context)
    {
        var steps = execution.StepExecutions.OfType<FunctionStepExecution>().ToArray();
        if (steps.Length == 0)
        {
            return;
        }
        var ids = steps.Select(e => e.FunctionAppId).Distinct().ToArray();
        var items = await context.FunctionApps.Where(x => ids.Contains(x.FunctionAppId)).ToArrayAsync();
        var matches = steps.Join(items, s => s.FunctionAppId, x => x.FunctionAppId, (s, x) => (s, x));
        foreach (var (step, item) in matches)
        {
            step.FunctionApp = item;
        }
    }

    private static async Task MapQlikCloudClientsAsync(Execution execution, ExecutorDbContext context)
    {
        var steps = execution.StepExecutions.OfType<QlikStepExecution>().ToArray();
        if (steps.Length == 0)
        {
            return;
        }
        var ids = steps.Select(e => e.QlikCloudClientId).Distinct().ToArray();
        var items = await context.QlikCloudClients.Where(x => ids.Contains(x.QlikCloudClientId)).ToArrayAsync();
        var matches = steps.Join(items, s => s.QlikCloudClientId, x => x.QlikCloudClientId, (s, x) => (s, x));
        foreach (var (step, item) in matches)
        {
            step.QlikCloudClient = item;
        }
    }

    private static async Task MapPipelineClientsAsync(Execution execution, ExecutorDbContext context)
    {
        var steps = execution.StepExecutions.OfType<PipelineStepExecution>().ToArray();
        if (steps.Length == 0)
        {
            return;
        }
        var ids = steps.Select(e => e.PipelineClientId).Distinct().ToArray();
        var items = await context.PipelineClients
            .Where(x => ids.Contains(x.PipelineClientId))
            .Include(x => x.AppRegistration)
            .ToArrayAsync();
        var matches = steps.Join(items, s => s.PipelineClientId, x => x.PipelineClientId, (s, x) => (s, x));
        foreach (var (step, item) in matches)
        {
            step.PipelineClient = item;
        }
    }

    private static async Task MapConnectionsAsync(Execution execution, ExecutorDbContext context)
    {
        var steps = execution.StepExecutions.OfType<IHasConnection>().ToArray();
        if (steps.Length == 0)
        {
            return;
        }
        var ids = steps.Select(e => e.ConnectionId).Distinct().ToArray();
        var items = await context.Connections.Where(x => ids.Contains(x.ConnectionId)).ToArrayAsync();
        var matches = steps.Join(items, s => s.ConnectionId, x => x.ConnectionId, (s, x) => (s, x));
        foreach (var (step, item) in matches)
        {
            if (step is IHasConnection<SqlConnectionInfo> sqlStep && item is SqlConnectionInfo sqlConn)
            {
                sqlStep.Connection = sqlConn;
                continue;
            }
            if (step is IHasConnection<AnalysisServicesConnectionInfo> asStep && item is AnalysisServicesConnectionInfo asConn)
            {
                asStep.Connection = asConn;
                continue;
            }
            throw new ApplicationException($"Unhandled step and connection type pairing: step id {((StepExecution)step).StepId}, connection id {item.ConnectionId}");
        }
    }
}
