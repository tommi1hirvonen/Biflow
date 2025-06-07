namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record UpdateJob
{
    public required string JobName { get; init; }
    public required string? JobDescription { get; init; }
    public required ExecutionMode ExecutionMode { get; init; }
    public required bool StopOnFirstError { get; init; }
    public required double OvertimeNotificationLimitMinutes { get; init; }
    public required double TimeoutMinutes { get; init; }
    public Guid[] JobTagIds { get; init; } = [];
}