using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddExecutionBuilderFactory<TDbContext>(this IServiceCollection services)
        where TDbContext : AppDbContext
    {
        services.AddSingleton(typeof(IExecutionBuilderFactory<TDbContext>), typeof(ExecutionBuilderFactory<TDbContext>));
        return services;
    }

    public static IServiceCollection AddDuplicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<StepsDuplicatorFactory>();
        services.AddSingleton<JobDuplicatorFactory>();
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
            .ThenInclude(e => e.DependantOnStepExecution)
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
            join sql in context.SqlConnections
                on new { Id = includeEndpoint ? (object?)((SqlStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)sql.ConnectionId : false }
                into sql_
            from sql in sql_.DefaultIfEmpty()
            join package in context.SqlConnections
                on new { Id = includeEndpoint ? (object?)((PackageStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)package.ConnectionId : false }
                into package_
            from package in package_.DefaultIfEmpty()
            join agent in context.SqlConnections
                on new { Id = includeEndpoint ? (object?)((AgentJobStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)agent.ConnectionId : false }
                into agent_
            from agent in agent_.DefaultIfEmpty()
            join tabular in context.AnalysisServicesConnections
                on new { Id = includeEndpoint ? (object?)((TabularStepExecution)stepExec).ConnectionId : true }
                equals new { Id = includeEndpoint ? (object?)tabular.ConnectionId : false }
                into tabular_
            from tabular in tabular_.DefaultIfEmpty()
            join dataset in context.AppRegistrations
                on new { Id = includeEndpoint ? (object?)((DatasetStepExecution)stepExec).AppRegistrationId : true }
                equals new { Id = includeEndpoint ? (object?)dataset.AppRegistrationId : false }
                into dataset_
            from dataset in dataset_.DefaultIfEmpty()
            join function in context.FunctionApps
                on new { Id = includeEndpoint ? (object?)((FunctionStepExecution)stepExec).FunctionAppId : true }
                equals new { Id = includeEndpoint ? (object?)function.FunctionAppId : false }
                into function_
            from function in function_.DefaultIfEmpty()
            join pipeline in context.PipelineClients.Include(a => a.AppRegistration)
                on new { Id = includeEndpoint ? (object?)((PipelineStepExecution)stepExec).PipelineClientId : true }
                equals new { Id = includeEndpoint ? (object?)pipeline.PipelineClientId : false }
                into pipeline_
            from pipeline in pipeline_.DefaultIfEmpty()
            join qlik in context.QlikCloudClients
                on new { Id = includeEndpoint ? (object?)((QlikStepExecution)stepExec).QlikCloudClientId : true }
                equals new { Id = includeEndpoint ? (object?)qlik.QlikCloudClientId : false }
                into qlik_
            from qlik in qlik_.DefaultIfEmpty()
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
                function,
                pipeline,
                qlik,
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
                    dataset.SetAppRegistration(step.DatasetStepAppRegistration);
                    break;
                case FunctionStepExecution function:
                    function.SetApp(step.FunctionStepApp);
                    break;
                case PipelineStepExecution pipeline:
                    pipeline.SetClient(step.PipelineStepClient);
                    break;
                case QlikStepExecution qlik:
                    qlik.SetClient(step.QlikStepClient);
                    break;
                default:
                    break;
            }
        }

        return execution;
    }

    internal static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}

file record StepExecutionProjection(
    StepExecution StepExecution,
    SqlConnectionInfo? SqlStepConnection,
    SqlConnectionInfo? PackageStepConnection,
    SqlConnectionInfo? AgentJobStepConnection,
    AnalysisServicesConnectionInfo? TabularStepConnection,
    AppRegistration? DatasetStepAppRegistration,
    FunctionApp? FunctionStepApp,
    PipelineClient? PipelineStepClient,
    QlikCloudClient? QlikStepClient,
    Step? Step);