namespace EtlManagerDataAccess.Models
{
    public record DatasetStepExecutionAttempt : StepExecutionAttempt
    {
        public DatasetStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Dataset)
        {
        }
    }
}
