namespace Biflow.Ui.Projections;

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
    StepExecutionStatus StepExecutionStatus,
    ExecutionStatus ExecutionStatus,
    ExecutionMode ExecutionMode,
    Guid? ScheduleId,
    string? ScheduleName,
    string JobName,
    Guid[] Dependencies,
    TagProjection[] StepTags,
    TagProjection[] JobTags) : IExecutionProjection, IStepExecutionProjection
{
    public virtual bool Equals(StepExecutionProjection? other) =>
        other is not null &&
        ExecutionId == other.ExecutionId &&
        StepId == other.StepId &&
        RetryAttemptIndex == other.RetryAttemptIndex;

    public override int GetHashCode() => base.GetHashCode();

    public double? ExecutionInSeconds { get; } = ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
    
    IReadOnlyCollection<ITag> IStepExecutionProjection.StepTags => StepTags; 

    public bool CanBeStopped =>
        StepExecutionStatus == StepExecutionStatus.Running
        || StepExecutionStatus == StepExecutionStatus.AwaitingRetry
        || StepExecutionStatus == StepExecutionStatus.Queued
        || StepExecutionStatus == StepExecutionStatus.NotStarted && ExecutionStatus == ExecutionStatus.Running;
}