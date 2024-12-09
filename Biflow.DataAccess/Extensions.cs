using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddExecutionBuilderFactory<TDbContext>(this IServiceCollection services, ServiceLifetime lifetime = default)
        where TDbContext : AppDbContext
    {
        var service = ServiceDescriptor.Describe(
            serviceType: typeof(IExecutionBuilderFactory<TDbContext>),
            implementationType: typeof(ExecutionBuilderFactory<TDbContext>),
            lifetime: lifetime);
        services.Add(service);
        return services;
    }

    public static void RegisterAzureKeyVaultColumnEncryptionKeyStoreProvider(IConfiguration configuration)
    {
        TokenCredential? credential = null;

        var section = configuration.GetSection("SqlColumnEncryptionAzureKeyVaultProvider");
        if (section.Exists())
        {
            var useSystemAssignedManagedIdentity = section.GetValue("UseSystemAssignedManagedIdentity", false);
            var userAssignedManagedIdentityClientId = section.GetValue<string?>("UserAssignedManagedIdentityClientId");
            var spSection = section.GetSection("ServicePrincipal");
            if (useSystemAssignedManagedIdentity)
            {
                credential = new ManagedIdentityCredential();
            }
            else if (userAssignedManagedIdentityClientId is not null)
            {
                credential = new ManagedIdentityCredential(userAssignedManagedIdentityClientId);
            }
            else if (spSection.Exists())
            {
                var tenantId = spSection.GetValue<string>("TenantId");
                var clientId = spSection.GetValue<string>("ClientId");
                var clientSecret = spSection.GetValue<string>("ClientSecret");
                credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            }
        }

        if (credential is null)
        {
            return;
        }

        var keyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(credential);
        var customProviders = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
        {
            { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, keyVaultProvider }
        };
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: customProviders);
    }

    public static IServiceCollection AddDuplicatorServices(this IServiceCollection services)
    {
        services.AddScoped<StepsDuplicatorFactory>();
        services.AddScoped<JobDuplicatorFactory>();
        return services;
    }

    /// <summary>
    /// Query execution and include the entire navigation graph. This includes step executions, attempts and parameters etc.
    /// Step execution endpoint clients are also included (e.g. <see cref="SqlStepExecution.GetConnection"/>).
    /// </summary>
    /// <param name="context"><see cref="AppDbContext"/> instance to query</param>
    /// <param name="executionId">id of the execution to get</param>
    /// <param name="includeEndpoint">Include step execution endpoints (e.g. connections) so that calls such as <see cref="SqlStepExecution.GetConnection"/>
    /// return the current endpoint if it has not been deleted from the db</param>
    /// <param name="includeStep">Include step navigation so that calling <see cref="StepExecution.GetStep"/>
    /// returns the current step if it has not been deleted from the db</param>
    /// <returns><see cref="Execution"/> with all navigation properties included (incl. step execution endpoint clients, e.g. connections). <see langword="null"/> if not found.</returns>
    public static async Task<Execution?> GetExecutionWithEntireGraphAsync(
        this AppDbContext context,
        Guid executionId,
        bool includeEndpoint = false,
        bool includeStep = false)
    {
        // Use tracking queries and let EF match the entities from the two separate queries.

        var execution = await context.Executions
            .Include(e => e.ExecutionParameters)
            .Include(e => e.ExecutionConcurrencies)
            .FirstOrDefaultAsync(e => e.ExecutionId == executionId);

        if (execution is null)
        {
            return null;
        }

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
        // In case the respective include was disabled,
        // use join conditions which SQL Server can see always evaluate to false.
        // This way the left join is completely excluded from the actual query plan
        // even if it is present in the SQL query => no performance penalty.
        var query2 =
            from stepExec in query1
            join sql in context.SqlConnections.Include(c => (c as MsSqlConnection)!.Credential)
                on new { Id = includeEndpoint ? (object?)((SqlStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)sql.ConnectionId : false }
                into sql_
            from sql in sql_.DefaultIfEmpty()
            join package in context.MsSqlConnections.Include(c => c.Credential)
                on new { Id = includeEndpoint ? (object?)((PackageStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)package.ConnectionId : false }
                into package_
            from package in package_.DefaultIfEmpty()
            join agent in context.MsSqlConnections.Include(c => c.Credential)
                on new { Id = includeEndpoint ? (object?)((AgentJobStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)agent.ConnectionId : false }
                into agent_
            from agent in agent_.DefaultIfEmpty()
            join tabular in context.AnalysisServicesConnections.Include(c => c.Credential)
                on new { Id = includeEndpoint ? (object?)((TabularStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)tabular.ConnectionId : false }
                into tabular_
            from tabular in tabular_.DefaultIfEmpty()
            join dataset in context.AzureCredentials
                on new { Id = includeEndpoint ? (object?)((DatasetStepExecution)stepExec).AzureCredentialId : true }
                equals new { Id = includeEndpoint ? (object?)dataset.AzureCredentialId : false }
                into dataset_
            from dataset in dataset_.DefaultIfEmpty()
            join dataflow in context.AzureCredentials
                on new { Id = includeEndpoint ? (object?)((DataflowStepExecution)stepExec).AzureCredentialId : true }
                equals new { Id = includeEndpoint ? (object?)dataflow.AzureCredentialId : false }
                into dataflow_
            from dataflow in dataflow_.DefaultIfEmpty()
            join fabric in context.AzureCredentials
                on new { Id = includeEndpoint ? (object?)((FabricStepExecution)stepExec).AzureCredentialId : true }
                equals new { Id = includeEndpoint ? (object?)fabric.AzureCredentialId : false }
                into fabric_
            from fabric in fabric_.DefaultIfEmpty()
            join function in context.FunctionApps
                on new { Id = includeEndpoint ? (object?)((FunctionStepExecution)stepExec).FunctionAppId : true }
                equals new { Id = includeEndpoint ? (object?)function.FunctionAppId : false }
                into function_
            from function in function_.DefaultIfEmpty()
            join pipeline in context.PipelineClients.Include(a => a.AzureCredential)
                on new { Id = includeEndpoint ? (object?)((PipelineStepExecution)stepExec).PipelineClientId : true }
                equals new { Id = includeEndpoint ? (object?)pipeline.PipelineClientId : false }
                into pipeline_
            from pipeline in pipeline_.DefaultIfEmpty()
            join qlik in context.QlikCloudEnvironments
                on new { Id = includeEndpoint ? (object?)((QlikStepExecution)stepExec).QlikCloudEnvironmentId : true }
                equals new { Id = includeEndpoint ? (object?)qlik.QlikCloudEnvironmentId : false }
                into qlik_
            from qlik in qlik_.DefaultIfEmpty()
            join db in context.DatabricksWorkspaces
                on new { Id = includeEndpoint ? (object?)((DatabricksStepExecution)stepExec).DatabricksWorkspaceId : true }
                equals new { Id = includeEndpoint ? (object?)db.WorkspaceId : false }
                into db_
            from db in db_.DefaultIfEmpty()
            join dbt in context.DbtAccounts
                on new { Id = includeEndpoint ? (object?)((DbtStepExecution)stepExec).DbtAccountId : true }
                equals new { Id = includeEndpoint ? (object?)dbt.DbtAccountId : false }
                into dbt_
            from dbt in dbt_.DefaultIfEmpty()
            join scd in context.ScdTables.Include(t => t.Connection).ThenInclude(c => (c as MsSqlConnection)!.Credential)
                on new { Id = includeEndpoint ? (object?)((ScdStepExecution)stepExec).ScdTableId : true }
                equals new { Id = includeEndpoint ? (object?)scd.ScdTableId : false }
                into scd_
            from scd in scd_.DefaultIfEmpty()
            join exe in context.Credentials
                on new { Id = includeEndpoint ? (object?)((ExeStepExecution)stepExec).RunAsCredentialId : true }
                equals new { Id = includeEndpoint ? (object?)exe.CredentialId : false }
                into exe_
            from exe in exe_.DefaultIfEmpty()
            join step in context.Steps
                on new { Id = includeStep ? (object?)stepExec.StepId : true }
                equals new { Id = includeStep ? (object?)step.StepId : false }
                into step_
            from step in step_.DefaultIfEmpty()
            select new StepExecutionProjection(
                stepExec,
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
                exe,
                step);

        var stepExecutions = await query2.ToArrayAsync();

        // Map endpoint clients to step executions.
        foreach (var step in stepExecutions)
        {
            step.StepExecution.SetStep(step.Step);
            switch (step.StepExecution)
            {
                case SqlStepExecution sql:
                    sql.SetConnection(step.SqlStepConnection);
                    break;
                case PackageStepExecution package:
                    package.SetConnection(step.PackageStepConnection);
                    break;
                case AgentJobStepExecution agent:
                    agent.SetConnection(step.AgentJobStepConnection);
                    break;
                case TabularStepExecution tabular:
                    tabular.SetConnection(step.TabularStepConnection);
                    break;
                case DatasetStepExecution dataset:
                    dataset.SetAzureCredential(step.DatasetStepAzureCredential);
                    break;
                case DataflowStepExecution dataflow:
                    dataflow.SetAzureCredential(step.DataflowStepAzureCredential);
                    break;
                case FabricStepExecution fabric:
                    fabric.SetAzureCredential(step.FabricStepAzureCredential);
                    break;
                case FunctionStepExecution function:
                    function.SetApp(step.FunctionStepApp);
                    break;
                case PipelineStepExecution pipeline:
                    pipeline.SetClient(step.PipelineStepClient);
                    break;
                case QlikStepExecution qlik:
                    qlik.SetEnvironment(step.QlikStepClient);
                    break;
                case DatabricksStepExecution db:
                    db.SetWorkspace(step.DatabricksWorkspace);
                    break;
                case DbtStepExecution dbt:
                    dbt.SetAccount(step.DbtAccount);
                    break;
                case ScdStepExecution scd:
                    scd.SetScdTable(step.ScdTable);
                    break;
                case ExeStepExecution exe:
                    exe.SetRunAsCredential(step.ExeStepCredential);
                    break;
            }
        }

        return execution;
    }
}

file record StepExecutionProjection(
    StepExecution StepExecution,
    SqlConnectionBase? SqlStepConnection,
    MsSqlConnection? PackageStepConnection,
    MsSqlConnection? AgentJobStepConnection,
    AnalysisServicesConnection? TabularStepConnection,
    AzureCredential? DatasetStepAzureCredential,
    AzureCredential? DataflowStepAzureCredential,
    AzureCredential? FabricStepAzureCredential,
    FunctionApp? FunctionStepApp,
    PipelineClient? PipelineStepClient,
    QlikCloudEnvironment? QlikStepClient,
    DatabricksWorkspace? DatabricksWorkspace,
    DbtAccount? DbtAccount,
    ScdTable? ScdTable,
    Credential? ExeStepCredential,
    Step? Step);