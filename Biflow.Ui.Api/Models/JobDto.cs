namespace Biflow.Ui.Api.Models;

public record JobDto(
    string JobName,
    string JobDescription,
    ExecutionMode ExecutionMode,
    bool StopOnFirstError,
    int MaxParallelSteps,
    double OvertimeNotificationLimitMinutes,
    double TimeoutMinutes,
    bool IsEnabled,
    bool IsPinned,
    Guid[] JobTagIds);