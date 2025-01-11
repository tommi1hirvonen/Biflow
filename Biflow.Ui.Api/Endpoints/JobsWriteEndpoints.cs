using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class JobsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("",
            async ([FromBody] JobDto jobDto, AppDbContext dbContext, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                if (await dbContext.Jobs.AnyAsync(j => j.JobId == jobDto.JobId, cancellationToken))
                {
                    throw new PrimaryKeyException<Job>(jobDto.JobId);
                }
                var job = jobDto.ToJob();
                var (results, isValid) = job.ValidateDataAnnotations();
                if (!isValid)
                {
                    throw new ValidationException(results);
                }
                dbContext.Jobs.Add(job);
                await dbContext.SaveChangesAsync(cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJob", new { jobId = jobDto.JobId });
                return Results.Created(url, job);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<Job>(StatusCodes.Status201Created)
            .WithDescription("Create a new job")
            .WithName("CreateJob");
        
        group.MapPut("",
            async ([FromBody] JobDto jobDto, AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var job = await dbContext.Jobs
                    .FirstOrDefaultAsync(j => j.JobId == jobDto.JobId, cancellationToken);
                if (job is null)
                {
                    throw new NotFoundException<Job>(jobDto.JobId);
                }
                dbContext.Entry(job).CurrentValues.SetValues(jobDto);
                var (results, isValid) = job.ValidateDataAnnotations();
                if (!isValid)
                {
                    throw new ValidationException(results);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Job>()
            .WithDescription("Update an existing job")
            .WithName("UpdateJob");
        
        group.MapDelete("{jobId:guid}", async (Guid jobId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobCommand(jobId);
                var job = await mediator.SendAsync(command, cancellationToken);
                if (job is null)
                {
                    throw new NotFoundException<Job>(jobId);
                }
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Job>()
            .WithDescription("Delete a job")
            .WithName("DeleteJob");
    }
}