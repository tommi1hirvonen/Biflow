using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepProcessingListener
{
    public Task OnPreExecuteAsync(ExtendedCancellationTokenSource cts);

    public Task OnPostExecuteAsync();
}
