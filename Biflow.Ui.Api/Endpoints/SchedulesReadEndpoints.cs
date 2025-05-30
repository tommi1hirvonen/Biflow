namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class SchedulesReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.SchedulesRead]);

        var group = app.MapGroup("/jobs/schedules")
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
            .Produces<Schedule[]>()
            .WithSummary("Get all schedules")
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
                if (schedule is null)
                {
                    throw new NotFoundException<Schedule>(scheduleId);
                }
                return Results.Ok(schedule);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Schedule>()
            .WithSummary("Get schedule by id")
            .WithDescription("Get schedule by id")
            .WithName("GetSchedule");
        
        group.MapGet("/tags", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.ScheduleTags.AsNoTracking().ToArrayAsync(cancellationToken);
                return tags;
            })
            .Produces<ScheduleTag[]>()
            .WithSummary("Get all schedule tags")
            .WithDescription("Get all schedule tags")
            .WithName("GetScheduleTags");
        
        group.MapGet("/tags/{tagId:guid}", async (Guid tagId, ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tag = await dbContext.ScheduleTags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken)
                    ?? throw new NotFoundException<ScheduleTag>(tagId);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<ScheduleTag>()
            .WithSummary("Get schedule tag by id")
            .WithDescription("Get schedule tag by id")
            .WithName("GetScheduleTag");
    }
}