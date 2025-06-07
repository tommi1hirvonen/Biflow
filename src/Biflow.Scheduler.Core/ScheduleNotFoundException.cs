namespace Biflow.Scheduler.Core;

public class ScheduleNotFoundException(SchedulerSchedule schedule)
    : Exception($"No matching schedule found for job id {schedule.JobId} and schedule id {schedule.ScheduleId}");