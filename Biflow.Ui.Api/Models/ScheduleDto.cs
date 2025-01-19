namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record ScheduleDto(
    string ScheduleName,
    string CronExpression,
    bool IsEnabled,
    bool DisallowConcurrentExecution,
    Guid[] ScheduleTagIds,
    Guid[] FilterStepTagIds);