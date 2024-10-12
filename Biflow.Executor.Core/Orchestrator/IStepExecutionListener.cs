using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepExecutionListener
{
    public Task OnPreExecuteAsync(StepExecution stepExecution, ExtendedCancellationTokenSource cts);

    public Task OnPostExecuteAsync(StepExecution stepExecution);
}
