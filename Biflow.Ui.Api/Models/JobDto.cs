namespace Biflow.Ui.Api.Models;

public record JobDto(
    Guid JobId,
    string JobName,
    string JobDescription,
    ExecutionMode ExecutionMode,
    bool StopOnFirstError,
    int MaxParallelSteps,
    double OvertimeNotificationLimitMinutes,
    double TimeoutMinutes,
    bool IsEnabled,
    bool IsPinned)
{
    public Job ToJob() => new()
    {
        JobId = JobId,
        JobName = JobName,
        JobDescription = JobDescription,
        ExecutionMode = ExecutionMode,
        StopOnFirstError = StopOnFirstError,
        MaxParallelSteps = MaxParallelSteps,
        OvertimeNotificationLimitMinutes = OvertimeNotificationLimitMinutes,
        TimeoutMinutes = TimeoutMinutes,
        IsEnabled = IsEnabled,
        IsPinned = IsPinned
    };
}