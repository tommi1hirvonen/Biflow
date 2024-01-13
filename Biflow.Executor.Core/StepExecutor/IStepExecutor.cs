using Biflow.Core.Entities;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.StepExecutor;

internal interface IStepExecutor<TAttempt>
    where TAttempt : StepExecutionAttempt
{
    public Task<Result> ExecuteAsync(TAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource);

    public TAttempt Clone(TAttempt other, int retryAttemptIndex);
}
