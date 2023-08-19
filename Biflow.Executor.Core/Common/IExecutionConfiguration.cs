namespace Biflow.Executor.Core.Common;

public interface IExecutionConfiguration
{
    public int MaxParallelSteps { get; }

    public int PollingIntervalMs { get; }
}
