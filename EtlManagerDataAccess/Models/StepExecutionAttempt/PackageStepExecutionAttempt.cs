namespace EtlManagerDataAccess.Models
{
    public record PackageStepExecutionAttempt : StepExecutionAttempt
    {
        
        public PackageStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Package)
        {
        }
    
        public long? PackageOperationId { get; set; }

        public override void Reset()
        {
            base.Reset();
            PackageOperationId = null;
        }
    }
}
