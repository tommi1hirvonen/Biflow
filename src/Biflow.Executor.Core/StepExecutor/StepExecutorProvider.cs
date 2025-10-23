using static Microsoft.Extensions.DependencyInjection.ActivatorUtilities;

namespace Biflow.Executor.Core.StepExecutor;

internal class StepExecutorProvider(IServiceProvider services) : IStepExecutorProvider
{
    public IStepExecutor GetExecutorFor(StepExecution step, StepExecutionAttempt attempt) => (step, attempt) switch
    {
        (AgentJobStepExecution, AgentJobStepExecutionAttempt) => CreateInstance<AgentJobStepExecutor>(services, step, attempt),
        (DatabricksStepExecution, DatabricksStepExecutionAttempt) => CreateInstance<DatabricksStepExecutor>(services, step, attempt),
        (DataflowStepExecution, DataflowStepExecutionAttempt) => CreateInstance<DataflowStepExecutor>(services, step, attempt),
        (DatasetStepExecution, DatasetStepExecutionAttempt) => CreateInstance<DatasetStepExecutor>(services, step, attempt),
        (DbtStepExecution, DbtStepExecutionAttempt) => CreateInstance<DbtStepExecutor>(services, step, attempt),
        (EmailStepExecution, EmailStepExecutionAttempt) => CreateInstance<EmailStepExecutor>(services, step, attempt),
        (ExeStepExecution, ExeStepExecutionAttempt) => CreateInstance<ExeStepExecutor>(services, step, attempt),
        (FabricStepExecution, FabricStepExecutionAttempt) => CreateInstance<FabricStepExecutor>(services, step, attempt),
        (FunctionStepExecution, FunctionStepExecutionAttempt) => CreateInstance<FunctionStepExecutor>(services, step, attempt),
        (HttpStepExecution, HttpStepExecutionAttempt) => CreateInstance<HttpStepExecutor>(services, step, attempt),
        (JobStepExecution, JobStepExecutionAttempt) => CreateInstance<JobStepExecutor>(services, step, attempt),
        (PackageStepExecution, PackageStepExecutionAttempt) => CreateInstance<PackageStepExecutor>(services, step, attempt),
        (PipelineStepExecution, PipelineStepExecutionAttempt) => CreateInstance<PipelineStepExecutor>(services, step, attempt),
        (QlikStepExecution, QlikStepExecutionAttempt) => CreateInstance<QlikStepExecutor>(services, step, attempt),
        (ScdStepExecution, ScdStepExecutionAttempt) => CreateInstance<ScdStepExecutor>(services, step, attempt),
        (SqlStepExecution, SqlStepExecutionAttempt) => CreateInstance<SqlStepExecutor>(services, step, attempt),
        (TabularStepExecution, TabularStepExecutionAttempt) => CreateInstance<TabularStepExecutor>(services, step, attempt),
        _ => throw new InvalidOperationException("Error mapping step to an executor implementation. " +
                                                 "Unhandled combination of StepExecution and StepExecutionAttempt types: " +
                                                 $"({step.GetType()}, {attempt.GetType()})")
    };
}
