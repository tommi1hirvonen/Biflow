namespace Biflow.Executor.Core.StepExecutor;

internal class StepExecutorProvider(IServiceProvider services) : IStepExecutorProvider
{
    public IStepExecutor GetExecutorFor(StepExecution step, StepExecutionAttempt attempt) => (step, attempt) switch
    {
        (AgentJobStepExecution agentStep, AgentJobStepExecutionAttempt agentAttempt) =>
            new AgentJobStepExecutor(services, agentStep, agentAttempt),
        (DatabricksStepExecution dbxStep, DatabricksStepExecutionAttempt dbxAttempt) =>
            new DatabricksStepExecutor(services, dbxStep, dbxAttempt),
        (DataflowStepExecution dataflowStep, DataflowStepExecutionAttempt dataflowAttempt) =>
            new DataflowStepExecutor(services, dataflowStep, dataflowAttempt),
        (DatasetStepExecution datasetStep, DatasetStepExecutionAttempt datasetAttempt) =>
            new DatasetStepExecutor(services, datasetStep, datasetAttempt),
        (DbtStepExecution dbtStep, DbtStepExecutionAttempt dbtAttempt) =>
            new DbtStepExecutor(services, dbtStep, dbtAttempt),
        (EmailStepExecution emailStep, EmailStepExecutionAttempt emailAttempt) =>
            new EmailStepExecutor(services, emailStep, emailAttempt),
        (ExeStepExecution exeStep, ExeStepExecutionAttempt exeAttempt) when exeStep.GetProxy() is { } proxy =>
            new ProxyExeStepExecutor(services, exeStep, exeAttempt, proxy),
        (ExeStepExecution exeStep, ExeStepExecutionAttempt exeAttempt) =>
            new LocalExeStepExecutor(services, exeStep, exeAttempt),
        (FabricStepExecution fabricStep, FabricStepExecutionAttempt fabricAttempt) =>
            new FabricStepExecutor(services, fabricStep, fabricAttempt),
        (FunctionStepExecution functionStep, FunctionStepExecutionAttempt functionAttempt) =>
            new FunctionStepExecutor(services, functionStep, functionAttempt),
        (HttpStepExecution httpStep, HttpStepExecutionAttempt httpAttempt) =>
            new HttpStepExecutor(services, httpStep, httpAttempt),
        (JobStepExecution jobStep, JobStepExecutionAttempt jobAttempt) =>
            new JobStepExecutor(services, jobStep, jobAttempt),
        (PackageStepExecution packageStep, PackageStepExecutionAttempt packageAttempt) =>
            new PackageStepExecutor(services, packageStep, packageAttempt),
        (PipelineStepExecution pipelineStep, PipelineStepExecutionAttempt pipelineAttempt) =>
            new PipelineStepExecutor(services, pipelineStep, pipelineAttempt),
        (QlikStepExecution qlikStep, QlikStepExecutionAttempt qlikAttempt) =>
            new QlikStepExecutor(services, qlikStep, qlikAttempt),
        (ScdStepExecution scdStep, ScdStepExecutionAttempt scdAttempt) =>
            new ScdStepExecutor(services, scdStep, scdAttempt),
        (SqlStepExecution sqlStep, SqlStepExecutionAttempt sqlAttempt) when sqlStep.GetConnection() is MsSqlConnection msSql =>
            new MsSqlStepExecutor(services, sqlStep, sqlAttempt, msSql),
        (SqlStepExecution sqlStep, SqlStepExecutionAttempt sqlAttempt) when sqlStep.GetConnection() is SnowflakeConnection snowflake =>
            new SnowflakeSqlStepExecutor(services, sqlStep, sqlAttempt, snowflake),
        (SqlStepExecution sqlStep, _) =>
            throw new InvalidOperationException($"Unsupported connection type: {sqlStep.GetConnection()?.GetType().Name}. " +
                                                $"Connection must be of type {nameof(MsSqlConnection)} or {nameof(SnowflakeConnection)}."),
        (TabularStepExecution tabularStep, TabularStepExecutionAttempt tabularAttempt) =>
            new TabularStepExecutor(tabularStep, tabularAttempt),
        _ => throw new InvalidOperationException("Error mapping step to an executor implementation. " +
                                                 "Unhandled combination of StepExecution and StepExecutionAttempt types: " +
                                                 $"({step.GetType()}, {attempt.GetType()})")
    };
}
