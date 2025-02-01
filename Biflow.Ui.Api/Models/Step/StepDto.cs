namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public abstract record StepDto
{
    public required string StepName { get; init; }
    public string? StepDescription { get; init; }
    public required int ExecutionPhase { get; init; }
    public required DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; init; }
    public required bool IsEnabled { get; init; }
    public required int RetryAttempts { get; init; }
    public required double RetryIntervalMinutes { get; init; }
    public string? ExecutionConditionExpression { get; init; }
    public Guid[] StepTagIds { get; init; } = [];
    public DependencyDto[] Dependencies { get; init; } = [];
    public ExecutionConditionParameterDto[] ExecutionConditionParameters { get; init; } = [];
    public DataObjectRelationDto[] Sources { get; init; } = [];
    public DataObjectRelationDto[] Targets { get; init; } = [];
}