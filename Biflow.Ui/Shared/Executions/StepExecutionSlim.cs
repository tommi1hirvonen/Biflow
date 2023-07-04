using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared.Executions;

public record StepExecutionSlim(
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
    IList<Tag> Tags)
{
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;
}