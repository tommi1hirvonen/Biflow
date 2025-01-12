using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class JobsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsRead]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsRead)
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("",
            async (ServiceDbContext dbContext, CancellationToken cancellationToken, [FromQuery] bool includeTags = false) =>
            {
                var query = dbContext.Jobs
                    .AsNoTracking();
                if (includeTags)
                {
                    query = query.Include(t => t.Tags);
                }
                return await query
                    .OrderBy(j => !j.IsPinned)
                    .ThenBy(j => j.JobName)
                    .ToArrayAsync(cancellationToken);
            })
            .Produces<Job[]>()
            .WithDescription("Get all jobs. Collection properties " +
                             "(job parameters, concurrencies, steps, schedules) " +
                             "are not loaded and will be empty. " +
                             "Tags can be included by specifying the corresponding query parameter.")
            .WithName("GetJobs");
        
        group.MapGet("/tags", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.JobTags.AsNoTracking().ToArrayAsync(cancellationToken);
                return tags;
            })
            .Produces<JobTag[]>()
            .WithDescription("Get all job tags")
            .WithName("GetJobTags");
        
        group.MapGet("/tags/{tagId:guid}", async (Guid tagId, ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tag = await dbContext.JobTags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken)
                        ?? throw new NotFoundException<JobTag>(tagId);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<JobTag>()
            .WithDescription("Get job tag")
            .WithName("GetJobTag");

        group.MapGet("/{jobId:guid}", 
            async (ServiceDbContext dbContext,
                Guid jobId,
                CancellationToken cancellationToken,
                [FromQuery] bool includeParameters = false,
                [FromQuery] bool includeConcurrencies = false) => 
            {
                var query = dbContext.Jobs
                    .AsNoTracking()
                    .Include(j => j.Tags)
                    .AsQueryable();
                if (includeParameters)
                {
                    query = query.Include(j => j.JobParameters);
                }
                if (includeConcurrencies)
                {
                    query = query.Include(j => j.JobConcurrencies);
                }
                var job = await query
                    .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
                if (job is null)
                {
                    throw new NotFoundException<Job>(jobId);
                }
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Job>()
            .WithDescription("Get job by id. Steps and schedules are not loaded and will be empty. " +
                             "Job parameters and concurrency settings can be included by " +
                             "specifying the corresponding query parameters.")
            .WithName("GetJob");
        
        group.MapGet("/{jobId:guid}/steps",
            async (ServiceDbContext dbContext, Guid jobId, CancellationToken cancellationToken, [FromQuery] bool includeTags = false) =>
            {
                var jobExists = await dbContext.Jobs
                    .AnyAsync(j => j.JobId == jobId, cancellationToken);
                if (!jobExists)
                {
                    throw new NotFoundException<Job>(jobId);
                }
                var query = dbContext.Steps
                    .AsNoTracking()
                    .Where(s => s.JobId == jobId);
                if (includeTags)
                {
                    query = query.Include(s => s.Tags);
                }
                var steps = await query
                    .OrderBy(s => s.StepName)
                    .ToArrayAsync(cancellationToken);
                return Results.Ok(steps);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Step[]>()
            .WithDescription("Get all steps for a job. " +
                             "Step tags can be included by specifying the corresponding query parameter. " +
                             "Other collection properties are not loaded and will be empty.")
            .WithName("GetJobSteps");

        var jobSchedulesEndpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsRead, Scopes.SchedulesRead]);
        
        app.MapGet("/jobs/{jobId:guid}/schedules",
            async (ServiceDbContext dbContext, Guid jobId, CancellationToken cancellationToken) =>
            {
                var jobExists = await dbContext.Jobs
                    .AnyAsync(x => x.JobId == jobId, cancellationToken);
                if (!jobExists)
                {
                    throw new NotFoundException<Job>(jobId);
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
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Schedule[]>()
            .WithTags($"{Scopes.JobsRead}, {Scopes.SchedulesRead}")
            .WithDescription("Get all schedules for a job.")
            .WithName("GetJobSchedules")
            .AddEndpointFilter(jobSchedulesEndpointFilter);
    }
}