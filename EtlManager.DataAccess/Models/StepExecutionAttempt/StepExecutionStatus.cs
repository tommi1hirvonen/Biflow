namespace EtlManager.DataAccess.Models;

public enum StepExecutionStatus
{
    NotStarted,
    Running,
    Succeeded,
    Failed,
    Stopped,
    Skipped,
    AwaitRetry,
    Duplicate
}
