using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutorFactory(IServiceProvider serviceProvider, IDbContextFactory<ExecutorDbContext> dbContextFactory) : IJobExecutorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    public async Task<IJobExecutor> CreateAsync(Guid executionId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Use tracking queries and let EF match the entities from the two separate queries.
        
        var execution = await context.Executions
            .Include(e => e.ExecutionParameters)
            .Include(e => e.ExecutionConcurrencies)
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId)
            ?? throw new ExecutionNotFoundException(executionId);

        var query1 = context.StepExecutions
            .Where(e => e.ExecutionId == executionId)
            .Include(e => e.StepExecutionAttempts)
            .Include(e => e.ExecutionDependencies)
            .ThenInclude(e => e.DependantOnStepExecution)
            .Include(e => e.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(e => e.ExecutionConditionParameters)
            .ThenInclude(e => e.ExecutionParameter)
            .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
            .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}");

        // Left join endpoint clients to step executions.
        var query2 =
            from step in query1
            join sql in context.SqlConnections on ((SqlStepExecution)step).ConnectionId equals sql.ConnectionId into sql_
            from sql in sql_.DefaultIfEmpty()
            join package in context.SqlConnections on ((PackageStepExecution)step).ConnectionId equals package.ConnectionId into package_
            from package in package_.DefaultIfEmpty()
            join agent in context.SqlConnections on ((AgentJobStepExecution)step).ConnectionId equals agent.ConnectionId into agent_
            from agent in agent_.DefaultIfEmpty()
            join tabular in context.AnalysisServicesConnections on ((TabularStepExecution)step).ConnectionId equals tabular.ConnectionId into tabular_
            from tabular in tabular_.DefaultIfEmpty()
            join dataset in context.AppRegistrations on ((DatasetStepExecution)step).AppRegistrationId equals dataset.AppRegistrationId into dataset_
            from dataset in dataset_.DefaultIfEmpty()
            join function in context.FunctionApps on ((FunctionStepExecution)step).FunctionAppId equals function.FunctionAppId into function_
            from function in function_.DefaultIfEmpty()
            join pipeline in context.PipelineClients.Include(a => a.AppRegistration) on ((PipelineStepExecution)step).PipelineClientId equals pipeline.PipelineClientId into pipeline_
            from pipeline in pipeline_.DefaultIfEmpty()
            join qlik in context.QlikCloudClients on ((QlikStepExecution)step).QlikCloudClientId equals qlik.QlikCloudClientId into qlik_
            from qlik in qlik_.DefaultIfEmpty()
            select new
            {
                step,
                sql,
                package,
                agent,
                tabular,
                dataset,
                function,
                pipeline,
                qlik
            };

        var stepExecutions = await query2.ToArrayAsync();

        // Map endpoint clients to step executions.
        foreach (var step in stepExecutions)
        {
            switch (step.step)
            {
                case SqlStepExecution sql:
                    sql.SetConnection(step.sql);
                    break;
                case PackageStepExecution package:
                    package.SetConnection(step.package);
                    break;
                case AgentJobStepExecution agent:
                    agent.SetConnection(step.agent);
                    break;
                case TabularStepExecution tabular:
                    tabular.SetConnection(step.tabular);
                    break;
                case DatasetStepExecution dataset:
                    dataset.SetAppRegistration(step.dataset);
                    break;
                case FunctionStepExecution function:
                    function.SetApp(step.function);
                    break;
                case PipelineStepExecution pipeline:
                    pipeline.SetClient(step.pipeline);
                    break;
                case QlikStepExecution qlik:
                    qlik.SetClient(step.qlik);
                    break;
                default:
                    break;
            }
        }

        return ActivatorUtilities.CreateInstance<JobExecutor>(_serviceProvider, execution);
    }
}