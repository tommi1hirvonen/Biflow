namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public abstract record StepDto
{
    public required string StepName { get; init; }
    public required string? StepDescription { get; init; }
    public required int ExecutionPhase { get; init; }
    public required DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; init; }
    public required bool IsEnabled { get; init; }
    public required int RetryAttempts { get; init; }
    public required double RetryIntervalMinutes { get; init; }
    public required string? ExecutionConditionExpression { get; init; }
    public required Guid[] StepTagIds { get; init; }
    public required DependencyDto[] Dependencies { get; init; }
    public required ExecutionConditionParameterDto[] ExecutionConditionParameters { get; init; }
    public required DataObjectRelationDto[] Sources { get; init; }
    public required DataObjectRelationDto[] Targets { get; init; }
}