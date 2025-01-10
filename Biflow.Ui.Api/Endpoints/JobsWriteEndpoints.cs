using Biflow.Core.Constants;
using Biflow.Core.Entities;
using Biflow.Ui.Api.Models;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            async ([FromBody] JobDto jobDto, ServiceDbContext dbContext, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var job = jobDto.ToJob();
                var (results, isValid) = job.ValidateDataAnnotations();
                if (!isValid)
                {
                    var errors = results.ToDictionary();
                    return Results.ValidationProblem(errors);
                }
                dbContext.Jobs.Add(job);
                await dbContext.SaveChangesAsync(cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJob", new { jobId = jobDto.JobId });
                return Results.Created(url, job);
            })
            .Produces<Job>(StatusCodes.Status201Created)
            .WithDescription("Create a new job")
            .WithName("CreateJob");
        
        group.MapPut("",
            async ([FromBody] JobDto jobDto, ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var job = await dbContext.Jobs
                    .FirstOrDefaultAsync(j => j.JobId == jobDto.JobId, cancellationToken);
                if (job is null)
                {
                    return Results.NotFound();
                }
                dbContext.Entry(job).CurrentValues.SetValues(jobDto);
                var (results, isValid) = job.ValidateDataAnnotations();
                if (isValid)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return Results.Ok(job);
                }
                var errors = results.ToDictionary();
                return Results.ValidationProblem(errors);
            })
            .Produces(StatusCodes.Status404NotFound)
            .Produces<Job>()
            .WithDescription("Update an existing job")
            .WithName("UpdateJob");
        
        group.MapDelete("{jobId:guid}", async (Guid jobId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobCommand(jobId);
                var job = await mediator.SendAsync(command, cancellationToken);
                return job is null ? Results.NotFound() : Results.Ok(job);
            })
            .Produces(StatusCodes.Status404NotFound)
            .Produces<Job>()
            .WithDescription("Delete a job")
            .WithName("DeleteJob");
    }
}