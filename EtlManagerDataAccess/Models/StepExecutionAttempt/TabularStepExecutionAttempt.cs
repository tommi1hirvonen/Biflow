namespace EtlManagerDataAccess.Models
{
    public record TabularStepExecutionAttempt : StepExecutionAttempt
    {
        public TabularStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Tabular)
        {
        }
    }
}
