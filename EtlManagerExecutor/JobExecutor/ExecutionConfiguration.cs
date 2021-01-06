namespace EtlManagerExecutor
{
    internal readonly struct ExecutionConfiguration
    {
        public ExecutionConfiguration(string connectionString, string encryptionPassword, int maxParallelSteps, int pollingIntervalMs, string executionId, string jobId, bool notify)
        {
            ConnectionString = connectionString;
            EncryptionPassword = encryptionPassword;
            MaxParallelSteps = maxParallelSteps;
            PollingIntervalMs = pollingIntervalMs;
            ExecutionId = executionId;
            JobId = jobId;
            Notify = notify;
        }
        public readonly string ConnectionString { get; }
        public readonly string EncryptionPassword { get; }
        public readonly int MaxParallelSteps { get; }
        public readonly int PollingIntervalMs { get; }
        public readonly string ExecutionId { get; }
        public readonly string JobId { get; }
        public readonly bool Notify { get; }
    }
           
}
