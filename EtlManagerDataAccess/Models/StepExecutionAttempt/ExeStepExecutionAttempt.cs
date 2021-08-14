namespace EtlManagerDataAccess.Models
{
    public record ExeStepExecutionAttempt : StepExecutionAttempt
    {
        public ExeStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Exe)
        {
        }
    }
}
