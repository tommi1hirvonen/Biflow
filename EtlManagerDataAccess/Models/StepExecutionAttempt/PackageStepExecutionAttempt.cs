namespace EtlManagerDataAccess.Models
{
    public record PackageStepExecutionAttempt : StepExecutionAttempt
    {
        public long? PackageOperationId { get; set; }

        public override void Reset()
        {
            base.Reset();
            PackageOperationId = null;
        }
    }
}
