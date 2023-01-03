namespace Biflow.DataAccess.Models;

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
    AwaitRetry,
    Duplicate
}
