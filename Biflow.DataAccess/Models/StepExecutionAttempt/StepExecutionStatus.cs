namespace Biflow.DataAccess.Models;

public enum StepExecutionStatus
{
    NotStarted,
    Running,
    Succeeded,
    Warning,
    Failed,
    Stopped,
    Skipped,
    AwaitRetry,
    Duplicate
}
