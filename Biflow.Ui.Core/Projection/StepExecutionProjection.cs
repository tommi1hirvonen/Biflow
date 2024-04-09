namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight StepExecutionAttempt class replacement which can be used to only load selected attributes from the database using projection
/// </summary>
public record StepExecutionProjection(
    Guid ExecutionId,
    Guid StepId,
    int RetryAttemptIndex,
    string StepName,
    StepType StepType,
    int ExecutionPhase,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    StepExecutionStatus ExecutionStatus,
    ExecutionStatus ParentExecutionStatus,
    ExecutionMode ExecutionMode,
    Guid? ScheduleId,
    string? ScheduleName,
    Guid? JobId,
    string JobName,
    TagProjection[] StepTags,
    TagProjection[] JobTags)
{
    public virtual bool Equals(StepExecutionProjection? other) =>
        other is not null &&
        ExecutionId == other.ExecutionId &&
        StepId == other.StepId &&
        RetryAttemptIndex == other.RetryAttemptIndex;

    public override int GetHashCode() => base.GetHashCode();

    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;

    public bool CanBeStopped =>
        ExecutionStatus == StepExecutionStatus.Running
        || ExecutionStatus == StepExecutionStatus.AwaitingRetry
        || ExecutionStatus == StepExecutionStatus.Queued
        || ExecutionStatus == StepExecutionStatus.NotStarted && ParentExecutionStatus == Biflow.Core.Entities.ExecutionStatus.Running;
}