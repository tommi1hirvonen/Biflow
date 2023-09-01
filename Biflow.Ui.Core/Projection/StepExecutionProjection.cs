using Biflow.DataAccess.Models;

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
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime,
    StepExecutionStatus ExecutionStatus,
    ExecutionStatus ParentExecutionStatus,
    bool DependencyMode,
    Guid? ScheduleId,
    Guid? JobId,
    string JobName,
    Tag[] Tags)
{
    public virtual bool Equals(StepExecutionProjection? other) =>
        other is not null &&
        ExecutionId == other.ExecutionId &&
        StepId == other.StepId &&
        RetryAttemptIndex == other.RetryAttemptIndex;

    public override int GetHashCode() => base.GetHashCode();

    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;
}