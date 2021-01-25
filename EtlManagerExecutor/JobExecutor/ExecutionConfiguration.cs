namespace EtlManagerExecutor
{
    internal record ExecutionConfiguration(
        string ConnectionString,
        string EncryptionPassword,
        int MaxParallelSteps,
        int PollingIntervalMs,
        string ExecutionId,
        string JobId,
        bool Notify);
}
