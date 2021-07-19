namespace EtlManagerDataAccess.Models
{
    public record PipelineStepExecutionAttempt : StepExecutionAttempt
    {
        public string? PipelineRunId { get; set; }

        public override void Reset()
        {
            base.Reset();
            PipelineRunId = null;
        }
    }
}
