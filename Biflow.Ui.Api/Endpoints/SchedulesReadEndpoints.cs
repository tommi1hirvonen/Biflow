using Biflow.Core.Constants;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class SchedulesReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.SchedulesRead]);
        
        app.MapGet("/jobs/{jobId:guid}/schedules",
            async (ServiceDbContext dbContext, Guid jobId, CancellationToken cancellationToken) =>
            {
                var jobExists = await dbContext.Jobs
                    .AnyAsync(x => x.JobId == jobId, cancellationToken);
                if (!jobExists)
                {
                    return Results.NotFound();
                }
                var schedules = await dbContext.Schedules
                    .AsNoTracking()
                    .Where(s => s.JobId == jobId)
                    .Include(s => s.TagFilter)
                    .Include(s => s.Tags)
                    .OrderBy(s => s.ScheduleName)
                    .ToArrayAsync(cancellationToken);
                return Results.Ok(schedules);
            })
            .WithTags(Scopes.SchedulesRead)
            .WithDescription("Get all schedules for a job.")
            .WithName("GetJobSchedules")
            .AddEndpointFilter(endpointFilter);

        var group = app.MapGroup("/schedules")
            .WithTags(Scopes.SchedulesRead)
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.Schedules
                    .AsNoTracking()
                    .Include(s => s.Tags)
                    .Include(s => s.TagFilter)
                    .OrderBy(s => s.JobId)
                    .ThenBy(s => s.ScheduleName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all schedules")
            .WithName("GetSchedules");
        
        group.MapGet("/{scheduleId:guid}",
            async (ServiceDbContext dbContext, Guid scheduleId, CancellationToken cancellationToken) =>
            {
                var schedule = await dbContext.Schedules
                    .AsNoTracking()
                    .Include(s => s.Tags)
                    .Include(s => s.TagFilter)
                    .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId, cancellationToken);
                return schedule is null ? Results.NotFound() : Results.Ok(schedule);
            })
            .WithDescription("Get schedule by id")
            .WithName("GetSchedule");
    }
}