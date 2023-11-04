using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepOrchestrator
{
    public Task<bool> RunAsync(StepExecution stepExecution, ExtendedCancellationTokenSource cancellationTokenSource);
}
