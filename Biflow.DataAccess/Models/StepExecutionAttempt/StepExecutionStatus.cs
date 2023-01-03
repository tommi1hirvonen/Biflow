namespace Biflow.DataAccess.Models;

public enum StepExecutionStatus
{
    NotStarted,
    Queued,
    Running,
    Succeeded,
    Warning,
    Failed,
    Stopped,
    Skipped,
    DependenciesFailed,
    AwaitRetry,
    Duplicate
}
