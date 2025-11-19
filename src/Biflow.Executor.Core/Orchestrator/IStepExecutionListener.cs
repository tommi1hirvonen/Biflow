namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepExecutionListener
{
    public Task OnPreExecuteAsync(StepExecution stepExecution, CancellationContext cancellationContext);

    public Task OnPostExecuteAsync(StepExecution stepExecution);
}
