namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight Execution class replacement which can be used to only load selected attributes from the database using projection
/// </summary>
public record ExecutionProjection(
    Guid ExecutionId,
    Guid? JobId,
    string JobName,
    Guid? ScheduleId,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    ExecutionStatus ExecutionStatus,
    int StepExecutionCount)
{
    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}
