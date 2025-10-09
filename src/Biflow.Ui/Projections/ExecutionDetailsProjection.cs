namespace Biflow.Ui.Projections;

public record ExecutionDetailsProjection(
    Guid ExecutionId,
    Guid JobId,
    string JobName,
    Guid? ScheduleId,
    string? ScheduleName,
    string? CronExpression,
    string? CreatedBy,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    ExecutionMode ExecutionMode,
    ExecutionStatus ExecutionStatus,
    int? ExecutorProcessId,
    bool StopOnFirstError,
    int MaxParallelSteps,
    double TimeoutMinutes,
    double OvertimeNotificationLimitMinutes,
    StepExecutionAttemptReference? ParentExecution)
{
    public string? GetDurationInReadableFormat() => ExecutionInSeconds?.SecondsToReadableFormat();
    
    private double? ExecutionInSeconds { get; } = ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}