using JetBrains.Annotations;
using System.ComponentModel.DataAnnotations;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class WaitStepExecution : StepExecution, IHasStepExecutionAttempts<WaitStepExecutionAttempt>
{
    public WaitStepExecution(string stepName) : base(stepName, StepType.Wait)
    {
    }

    public WaitStepExecution(WaitStep step, Execution execution) : base(step, execution)
    {
        WaitSeconds = step.WaitSeconds;
        AddAttempt(new WaitStepExecutionAttempt(this));
    }

    [Range(1, 604800)]
    public int WaitSeconds { get; [UsedImplicitly] private set; }

    public override DisplayStepType DisplayStepType => DisplayStepType.Wait;

    public override WaitStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new WaitStepExecutionAttempt((WaitStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }
}