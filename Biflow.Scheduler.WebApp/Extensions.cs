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
            try
            {
                await schedulesManager.RemoveScheduleAsync(schedule, CancellationToken.None);
                return Results.Ok();
            }
            catch (ScheduleNotFoundException)
            {
                return Results.NotFound();
            }
        }).WithName("Remove schedule");


        app.MapPost("/schedules/update", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            try
            {
                await schedulesManager.UpdateScheduleAsync(schedule, CancellationToken.None);
                return Results.Ok();
            }
            catch (ScheduleNotFoundException)
            {
                return Results.NotFound();
            }
        }).WithName("Update schedule");


        app.MapPost("/schedules/removejob", async (SchedulerJob job, ISchedulesManager schedulesManager) =>
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


        app.MapGet("/schedules/synchronize", async (ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.ReadAllSchedulesAsync(CancellationToken.None);
        }).WithName("Synchronize");


        app.MapGet("/schedules/status", async (ISchedulesManager schedulesManager, CancellationToken cancellationToken) =>
        {
            return schedulesManager.DatabaseReadError
                ? throw new ApplicationException("Scheduler is running but has encountered a database read error.")
                : Results.Ok(await schedulesManager.GetStatusAsync(cancellationToken));
        }).WithName("Status");

        return app;
    }
}
