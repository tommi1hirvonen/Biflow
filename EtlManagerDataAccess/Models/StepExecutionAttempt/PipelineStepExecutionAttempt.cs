namespace EtlManagerDataAccess.Models
{
    public record PipelineStepExecutionAttempt : StepExecutionAttempt
    {
        
        public PipelineStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Pipeline)
        {
        }
    
        public string? PipelineRunId { get; set; }

        public override void Reset()
        {
            base.Reset();
            PipelineRunId = null;
        }
    }
}
