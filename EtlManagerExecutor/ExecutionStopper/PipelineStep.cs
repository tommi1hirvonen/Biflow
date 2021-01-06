namespace EtlManagerExecutor
{
    partial class ExecutionStopper
    {
        internal class PipelineStep
        {
            public string StepId { get; set; }
            public int RetryAttemptIndex { get; set; }
            public string PipelineRunId { get; set; }
            public string DataFactoryId { get; set; }
        }

    }

}
