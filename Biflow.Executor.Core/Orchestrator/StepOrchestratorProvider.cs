using Biflow.Executor.Core.StepExecutor;

namespace Biflow.Executor.Core.Orchestrator;

internal class StepOrchestratorProvider(
    StepOrchestrator<AgentJobStepExecution, AgentJobStepExecutionAttempt, AgentJobStepExecutor> agentJobOrchestrator,
    StepOrchestrator<DatasetStepExecution, DatasetStepExecutionAttempt, DatasetStepExecutor> datasetOrchestrator,
    StepOrchestrator<FunctionStepExecution, FunctionStepExecutionAttempt, DurableFunctionStepExecutor> durableOrchestrator,
    StepOrchestrator<EmailStepExecution, EmailStepExecutionAttempt, EmailStepExecutor> emailOrchestrator,
    StepOrchestrator<ExeStepExecution, ExeStepExecutionAttempt, ExeStepExecutor> exeOrchestrator,
    StepOrchestrator<FunctionStepExecution, FunctionStepExecutionAttempt, FunctionStepExecutor> functionOrchestrator,
    StepOrchestrator<JobStepExecution, JobStepExecutionAttempt, JobStepExecutor> jobOrchestrator,
    StepOrchestrator<PackageStepExecution, PackageStepExecutionAttempt, PackageStepExecutor> packageOrchestrator,
    StepOrchestrator<PipelineStepExecution, PipelineStepExecutionAttempt, PipelineStepExecutor> pipelineOrchestrator,
    StepOrchestrator<QlikStepExecution, QlikStepExecutionAttempt, QlikStepExecutor> qlikOrchestrator,
    StepOrchestrator<SqlStepExecution, SqlStepExecutionAttempt, SqlStepExecutor> sqlOrchestrator,
    StepOrchestrator<TabularStepExecution, TabularStepExecutionAttempt, TabularStepExecutor> tabularOrchestrator)
    : IStepOrchestratorProvider
{
    public IStepOrchestrator GetOrchestratorFor(StepExecution stepExecution) => stepExecution switch
    {
        AgentJobStepExecution => agentJobOrchestrator,
        DatasetStepExecution => datasetOrchestrator,
        FunctionStepExecution and { FunctionIsDurable: true } => durableOrchestrator,
        EmailStepExecution => emailOrchestrator,
        ExeStepExecution => exeOrchestrator,
        FunctionStepExecution => functionOrchestrator,
        JobStepExecution => jobOrchestrator,
        PackageStepExecution => packageOrchestrator,
        PipelineStepExecution => pipelineOrchestrator,
        QlikStepExecution => qlikOrchestrator,
        SqlStepExecution => sqlOrchestrator,
        TabularStepExecution => tabularOrchestrator,
        _ => throw new InvalidOperationException($"No matching step orchestrator found for type {stepExecution.GetType()}")
    };
}
