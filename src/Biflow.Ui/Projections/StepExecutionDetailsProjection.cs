namespace Biflow.Ui.Projections;

public record StepExecutionDetailsProjection(
    Guid ExecutionId,
    Guid StepId,
    int RetryAttemptIndex,
    string StepName,
    StepType StepType,
    int ExecutionPhase,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    StepExecutionStatus StepExecutionStatus,
    ExecutionStatus ExecutionStatus,
    ExecutionMode ExecutionMode,
    string JobName,
    Guid[] Dependencies,
    TagProjection[] StepTags) : IStepExecutionProjection
{
    public double? ExecutionInSeconds { get; } = ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
}