namespace Biflow.Scheduler.Core;

public record JobStatus(string JobId, IEnumerable<ScheduleStatus> Schedules);

public record ScheduleStatus(
    string ScheduleId,
    string? CronExpression,
    bool IsEnabled,
    bool IsRunning,
    bool DisallowConcurrentExecution);