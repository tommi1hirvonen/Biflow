namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record SqlStepDto(
    string StepName,
    string? StepDescription,
    int ExecutionPhase,
    DuplicateExecutionBehaviour DuplicateExecutionBehaviour,
    bool IsEnabled,
    int RetryAttempts,
    double RetryIntervalMinutes,
    string? ExecutionConditionExpression,
    Guid[] StepTagIds,
    int TimeoutMinutes,
    string SqlStatement,
    Guid ConnectionId,
    Guid? ResultCaptureJobParameterId);