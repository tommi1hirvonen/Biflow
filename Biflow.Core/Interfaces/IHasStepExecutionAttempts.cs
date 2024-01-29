using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepExecutionAttempts<TAttempt> where TAttempt : StepExecutionAttempt
{
    public TAttempt AddAttempt(StepExecutionStatus withStatus = default);
}
