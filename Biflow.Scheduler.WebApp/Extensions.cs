using Biflow.Scheduler.Core;

namespace Biflow.Scheduler.WebApp;

public static class Extensions
{
    public static WebApplication MapSchedulerEnpoints(this WebApplication app)
    {
        app.MapPost("/schedules/add", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.AddScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Add schedule");


        app.MapPost("/schedules/remove", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.RemoveScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Remove schedule");


        app.MapPost("/jobs/remove", async (SchedulerJob job, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.RemoveJobAsync(job, CancellationToken.None);
        }).WithName("Remove job");


        app.MapPost("schedules/pause", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Pause schedule");


        app.MapPost("/schedules/resume", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.ResumeScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Resume schedule");


        app.MapGet("/schedules/synchronize", async (ISchedulesManager schedulesManager, StatusTracker statusTracker) =>
        {
            try
            {
                await schedulesManager.ReadAllSchedules(CancellationToken.None);
                statusTracker.DatabaseReadError = false;
            }
            catch
            {
                statusTracker.DatabaseReadError = true;
                throw;
            }
        }).WithName("Synchronize");


        app.MapGet("/status", (StatusTracker statusTracker) =>
        {
            return statusTracker.DatabaseReadError
                ? throw new ApplicationException("Scheduler is running but has encountered a database read error.")
                : Results.Ok();
        }).WithName("Status");

        return app;
    }
}
