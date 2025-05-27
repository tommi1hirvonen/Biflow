using Biflow.ExecutorProxy.Core.Authentication;
using Biflow.Scheduler.Core;

namespace Biflow.Scheduler.WebApp;

public static class Endpoints
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
            })
            .WithSummary("Register a schedule with the scheduler service")
            .WithDescription("Register a new schedule with the scheduler service. " +
                             "Note that calling this endpoint will not create the schedule in the database.")
            .WithName("Add schedule");


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
            })
            .WithSummary("Remove a schedule from the scheduler service")
            .WithDescription("Remove/unregister a schedule from the scheduler service. " +
                             "Note that calling this endpoint will not delete the schedule from the database.")
            .WithName("Remove schedule");


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
            })
            .WithSummary("Update a schedule definition in the scheduler service")
            .WithDescription("Update the definition of an already registered schedule in the scheduler service. " +
                             "Note that calling this endpoint will not update the schedule in the database.")
            .WithName("Update schedule");


        schedules.MapPost("/removejob", async (SchedulerJob job, ISchedulesManager schedulesManager) =>
            {
                await schedulesManager.RemoveJobAsync(job, CancellationToken.None);
            })
            .WithSummary("Remove job and all associated schedules from the scheduler service")
            .WithDescription("Remove a job and all its associated schedules from the scheduler service. " +
                             "Note that calling this endpoint will not delete the job from the database.")
            .WithName("Remove job");


        schedules.MapPost("/pause", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
            {
                await schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
            })
            .WithSummary("Pause a schedule registered with the scheduler service")
            .WithDescription("Pause an already registered schedule. " +
                             "Note that calling this endpoint will not disable the schedule in the database.")
            .WithName("Pause schedule");


        schedules.MapPost("/resume", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
            {
                await schedulesManager.ResumeScheduleAsync(schedule, CancellationToken.None);
            })
            .WithSummary("Resume a schedule registered with the scheduler service")
            .WithDescription("Resume an already registered schedule. " +
                             "Note that calling this endpoint will not enable the schedule in the database.")
            .WithName("Resume schedule");


        schedules.MapGet("/synchronize", async (ISchedulesManager schedulesManager) => 
            { 
                await schedulesManager.ReadAllSchedulesAsync(CancellationToken.None); 
            })
            .WithSummary("Synchronize/reload all schedules from the database to the scheduler service")
            .WithDescription("Clear the scheduler service and reload all schedules from the database")
            .WithName("Synchronize");


        schedules.MapGet("/status", async (ISchedulesManager schedulesManager, CancellationToken cancellationToken) =>
        {
            var jobStatuses = await schedulesManager.GetStatusAsync(cancellationToken);
            return Results.Ok(jobStatuses);
        })
        .Produces<IEnumerable<JobStatus>>()
        .WithSummary("Get status of schedules")
        .WithDescription("Get the status of all schedules grouped by jobs")
        .WithName("Status");

        return app;
    }
}
