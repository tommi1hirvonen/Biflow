namespace EtlManager.Executor;

interface IJobExecutor
{
    Task RunAsync(Guid executionId, bool notify);
}
