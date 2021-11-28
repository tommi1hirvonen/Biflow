namespace EtlManager.Executor.Core.Common;

public interface IExecutionConfiguration
{
    public string ConnectionString { get; }

    public int MaxParallelSteps { get; }

    public int PollingIntervalMs { get; }
}
