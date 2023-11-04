namespace Biflow.DataAccess.Models;

public class TabularStepExecutionAttempt : StepExecutionAttempt
{
    public TabularStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Tabular)
    {
    }

    public TabularStepExecutionAttempt(TabularStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public TabularStepExecutionAttempt(TabularStepExecution execution) : base(execution) { }
}
