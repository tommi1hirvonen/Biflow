namespace Biflow.Core.Entities;

public enum StepExecutionStatus
{
    NotStarted,
    Queued,
    Running,
    Succeeded,
    Warning,
    Failed,
    Retry,
    Stopped,
    Skipped,
    DependenciesFailed,
    AwaitingRetry,
    Duplicate
}
