namespace EtlManagerExecutor
{
    partial class ExecutionStopper
    {
        internal class PackageStep
        {
            public string StepId { get; set; }
            public int RetryAttemptIndex { get; set; }
            public long PackageOperationId { get; set; }
            public string ConnectionString { get; set; }
        }

    }

}
