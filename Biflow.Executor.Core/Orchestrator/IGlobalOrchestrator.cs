using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IObservable<StepExecutionStatusInfo>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stepExecution"></param>
    /// <param name="onReadyForOrchestration"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IEnumerable<Task> RegisterStepExecutionsAsync(
        ICollection<(StepExecution Step, CancellationToken Token)> stepExecutions,
        Func<StepExecution, StepAction, Task> onReadyForOrchestration);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stepExecution"></param>
    /// <param name="onPreExecute">Invoked before the global constraints are checked and the step is executed</param>
    /// <param name="onPostExecute">Invoked in the finally block of the calling method</param>
    /// <param name="cts"></param>
    /// <returns>Task which completes when the step execution finishes</returns>
    public Task QueueAsync(
        StepExecution stepExecution,
        Func<ExtendedCancellationTokenSource, Task> onPreExecute,
        Func<Task> onPostExecute,
        ExtendedCancellationTokenSource cts);

    public void UpdateStatus(StepExecution step, OrchestrationStatus status);
}
