namespace Biflow.DataAccess.Models;

public class TabularStepExecutionAttempt : StepExecutionAttempt
{
    public TabularStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Tabular)
    {
    }

    protected TabularStepExecutionAttempt(TabularStepExecutionAttempt other) : base(other)
    {
    }

    protected override StepExecutionAttempt Clone() => new TabularStepExecutionAttempt(this);
}
