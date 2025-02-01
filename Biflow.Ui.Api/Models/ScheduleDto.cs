namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record ScheduleDto
{
    public required string ScheduleName { get; init; }
    public required string CronExpression { get; init; }
    public required bool IsEnabled { get; init; }
    public required bool DisallowConcurrentExecution { get; init; }
    public Guid[] ScheduleTagIds { get; init; } = [];
    public Guid[] FilterStepTagIds { get; init; } = [];
}
    