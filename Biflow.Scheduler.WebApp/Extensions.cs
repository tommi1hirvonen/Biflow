using Biflow.ExecutorProxy.Core.Authentication;
using Biflow.Scheduler.Core;

namespace Biflow.Scheduler.WebApp;

public static class Extensions
{
    public static WebApplication MapSchedulerEndpoints(this WebApplication app)
    {
        var schedules = app
            .MapGroup("/schedules")
            .WithName("Schedules")
            .AddEndpointFilter<ServiceApiKeyEndpointFilter>();


        schedules.MapPost("/add", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.AddScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Add schedule");


        schedules.MapPost("/remove", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
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


        schedules.MapPost("/update", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
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


        schedules.MapPost("/removejob", async (SchedulerJob job, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.RemoveJobAsync(job, CancellationToken.None);
        }).WithName("Remove job");


        schedules.MapPost("/pause", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Pause schedule");


        schedules.MapPost("/resume", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.ResumeScheduleAsync(schedule, CancellationToken.None);
        }).WithName("Resume schedule");


        schedules.MapGet("/synchronize", async (ISchedulesManager schedulesManager) =>
        {
            await schedulesManager.ReadAllSchedulesAsync(CancellationToken.None);
        }).WithName("Synchronize");


        schedules.MapGet("/status", async (ISchedulesManager schedulesManager, CancellationToken cancellationToken) =>
        {
            return schedulesManager.DatabaseReadException is not null
                ? throw new ApplicationException("Scheduler is running but has encountered a database read error.")
                : Results.Ok(await schedulesManager.GetStatusAsync(cancellationToken));
        }).WithName("Status");

        return app;
    }
}
