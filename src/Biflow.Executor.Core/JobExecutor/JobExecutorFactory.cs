using Biflow.Executor.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutorFactory(
    IServiceProvider serviceProvider,
    IDbContextFactory<ExecutorDbContext> dbContextFactory) : IJobExecutorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    public async Task<IJobExecutor> CreateAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // Use tracking queries and let EF match the entities from the two separate queries.
        
        var execution = await context.Executions
            .Include(e => e.ExecutionParameters)
            .Include(e => e.ExecutionConcurrencies)
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId, cancellationToken)
            ?? throw new ExecutionNotFoundException(executionId);

        var query1 = context.StepExecutions
            .Where(e => e.ExecutionId == executionId)
            .Include(e => e.StepExecutionAttempts)
            .Include(e => e.ExecutionDependencies)
            .Include(e => e.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(e => e.ExecutionConditionParameters)
            .ThenInclude(e => e.ExecutionParameter)
            .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
            .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}");

        // Left join endpoint clients to step executions.
        var query2 =
            from step in query1
            join sql in context.SqlConnections.Include(c => (c as MsSqlConnection)!.Credential) on ((SqlStepExecution)step).ConnectionId equals sql.ConnectionId into sql_
            from sql in sql_.DefaultIfEmpty()
            join package in context.MsSqlConnections.Include(c => c.Credential) on ((PackageStepExecution)step).ConnectionId equals package.ConnectionId into package_
            from package in package_.DefaultIfEmpty()
            join agent in context.MsSqlConnections.Include(c => c.Credential) on ((AgentJobStepExecution)step).ConnectionId equals agent.ConnectionId into agent_
            from agent in agent_.DefaultIfEmpty()
            join tabular in context.AnalysisServicesConnections.Include(c => c.Credential) on ((TabularStepExecution)step).ConnectionId equals tabular.ConnectionId into tabular_
            from tabular in tabular_.DefaultIfEmpty()
            join dataset in context.FabricWorkspaces.Include(f => f.AzureCredential) on ((DatasetStepExecution)step).FabricWorkspaceId equals dataset.FabricWorkspaceId into dataset_
            from dataset in dataset_.DefaultIfEmpty()
            join dataflow in context.FabricWorkspaces.Include(f => f.AzureCredential) on ((DataflowStepExecution)step).FabricWorkspaceId equals dataflow.FabricWorkspaceId into dataflow_
            from dataflow in dataflow_.DefaultIfEmpty()
            join fabric in context.FabricWorkspaces.Include(f => f.AzureCredential) on ((FabricStepExecution)step).FabricWorkspaceId equals fabric.FabricWorkspaceId into fabric_
            from fabric in fabric_.DefaultIfEmpty()
            join function in context.FunctionApps on ((FunctionStepExecution)step).FunctionAppId equals function.FunctionAppId into function_
            from function in function_.DefaultIfEmpty()
            join pipeline in context.PipelineClients.Include(a => a.AzureCredential) on ((PipelineStepExecution)step).PipelineClientId equals pipeline.PipelineClientId into pipeline_
            from pipeline in pipeline_.DefaultIfEmpty()
            join qlik in context.QlikCloudEnvironments on ((QlikStepExecution)step).QlikCloudEnvironmentId equals qlik.QlikCloudEnvironmentId into qlik_
            from qlik in qlik_.DefaultIfEmpty()
            join db in context.DatabricksWorkspaces on ((DatabricksStepExecution)step).DatabricksWorkspaceId equals db.WorkspaceId into db_
            from db in db_.DefaultIfEmpty()
            join dbt in context.DbtAccounts on ((DbtStepExecution)step).DbtAccountId equals dbt.DbtAccountId into dbt_
            from dbt in dbt_.DefaultIfEmpty()
            join scd in context.ScdTables.Include(t => t.Connection).ThenInclude(c => (c as MsSqlConnection)!.Credential) on ((ScdStepExecution)step).ScdTableId equals scd.ScdTableId into scd_
            from scd in scd_.DefaultIfEmpty()
            join exeCredential in context.Credentials on ((ExeStepExecution)step).RunAsCredentialId equals exeCredential.CredentialId into exeCredential_
            from exeCredential in exeCredential_.DefaultIfEmpty()
            join exeProxy in context.Proxies on ((ExeStepExecution)step).ProxyId equals exeProxy.ProxyId into exeProxy_
            from exeProxy in exeProxy_.DefaultIfEmpty()
            select new
            {
                step,
                sql,
                package,
                agent,
                tabular,
                dataset,
                dataflow,
                fabric,
                function,
                pipeline,
                qlik,
                db,
                dbt,
                scd,
                exeCredential,
                exeProxy
            };

        var stepExecutions = await query2.ToArrayAsync(cancellationToken);

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
                    dataset.SetFabricWorkspace(step.dataset);
                    break;
                case DataflowStepExecution dataflow:
                    dataflow.SetFabricWorkspace(step.dataflow);
                    break;
                case FabricStepExecution fabric:
                    fabric.SetFabricWorkspace(step.fabric);
                    break;
                case FunctionStepExecution function:
                    function.SetApp(step.function);
                    break;
                case PipelineStepExecution pipeline:
                    pipeline.SetClient(step.pipeline);
                    break;
                case QlikStepExecution qlik:
                    qlik.SetEnvironment(step.qlik);
                    break;
                case DatabricksStepExecution db:
                    db.SetWorkspace(step.db);
                    break;
                case DbtStepExecution dbt:
                    dbt.SetAccount(step.dbt);
                    break;
                case ScdStepExecution scd:
                    scd.SetScdTable(step.scd);
                    break;
                case ExeStepExecution exe:
                    exe.SetRunAsCredential(step.exeCredential);
                    exe.SetProxy(step.exeProxy);
                    break;
            }
        }

        return ActivatorUtilities.CreateInstance<JobExecutor>(_serviceProvider, execution);
    }
}