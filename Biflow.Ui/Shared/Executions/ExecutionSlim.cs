using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared.Executions;

public record ExecutionSlim(
    Guid ExecutionId,
    Guid? JobId,
    string JobName,
    Guid? ScheduleId,
    DateTimeOffset CreatedDateTime,
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime,
    ExecutionStatus ExecutionStatus,
    int StepExecutionCount)
{
    public double? ExecutionInSeconds => ((EndDateTime ?? DateTime.Now) - StartDateTime)?.TotalSeconds;
}
