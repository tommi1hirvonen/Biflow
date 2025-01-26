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
            .WithSummary("Get all jobs")
            .WithDescription("Get all jobs. Collection properties " +
                             "(job parameters, concurrencies, steps, schedules) " +
                             "are not loaded and will be empty. " +
                             "Tags can be included by specifying the corresponding query parameter.")
            .WithName("GetJobs");

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
            .WithSummary("Get job by id")
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
            .WithSummary("Get all steps for a given job")
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
            .WithSummary("Get all schedules for a given job")
            .WithDescription("Get all schedules for a job.")
            .WithName("GetJobSchedules")
            .AddEndpointFilter(jobSchedulesEndpointFilter);
        
        group.MapGet("/parameters/{parameterId:guid}",
            async (ServiceDbContext dbContext, Guid parameterId, CancellationToken cancellationToken) =>
            {
                var parameter = await dbContext.Set<JobParameter>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ParameterId == parameterId, cancellationToken)
                    ?? throw new NotFoundException<JobParameter>(parameterId);
                return Results.Ok(parameter);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<JobParameter>()
            .WithSummary("Get job parameter by id")
            .WithDescription("Get job parameter")
            .WithName("GetJobParameter");
        
        group.MapGet("/tags", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.JobTags.AsNoTracking().ToArrayAsync(cancellationToken);
                return tags;
            })
            .Produces<JobTag[]>()
            .WithSummary("Get all job tags")
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
            .WithSummary("Get job tag by id")
            .WithDescription("Get job tag by id")
            .WithName("GetJobTag");
        
        var dataObjectsGroup = app.MapGroup("/dataobjects")
            .WithTags(Scopes.JobsRead)
            .AddEndpointFilter(endpointFilter);
        
        dataObjectsGroup.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var dataObjects = await dbContext.DataObjects.AsNoTracking().ToArrayAsync(cancellationToken);
                return dataObjects;
            })
            .Produces<DataObject[]>()
            .WithSummary("Get all data objects")
            .WithDescription("Get all data objects")
            .WithName("GetDataObjects");
        
        dataObjectsGroup.MapGet("/{dataObjectId:guid}",
            async (Guid dataObjectId, ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var dataObject = await dbContext.DataObjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ObjectId == dataObjectId, cancellationToken)
                    ?? throw new NotFoundException<DataObject>(dataObjectId);
                return Results.Ok(dataObject);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DataObject>()
            .WithSummary("Get data object by id")
            .WithDescription("Get data object by id")
            .WithName("GetDataObject");
    }
}