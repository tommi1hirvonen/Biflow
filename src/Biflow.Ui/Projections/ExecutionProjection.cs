﻿namespace Biflow.Ui.Projections;

/// <summary>
/// Lightweight Execution class replacement which can be used to only load selected attributes from the database using projection
/// </summary>
public record ExecutionProjection(
    Guid ExecutionId,
    Guid JobId,
    string JobName,
    Guid? ScheduleId,
    string? ScheduleName,
    string? CreatedBy,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    ExecutionStatus ExecutionStatus,
    int StepExecutionCount,
    TagProjection[] JobTags) : IExecutionProjection
{
    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}
