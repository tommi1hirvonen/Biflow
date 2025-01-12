using Biflow.Ui.Api.Mediator.Commands;
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
            async ([FromBody] JobDto jobDto, IMediator mediator, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobCommand(jobDto);
                var job = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJob", new { jobId = jobDto.JobId });
                return Results.Created(url, job);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<Job>(StatusCodes.Status201Created)
            .WithDescription("Create a new job")
            .WithName("CreateJob");
        
        group.MapPut("",
            async ([FromBody] JobDto jobDto, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobCommand(jobDto);
                var job = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Job>()
            .WithDescription("Update an existing job")
            .WithName("UpdateJob");
        
        group.MapPost("/{jobId:guid}/tags/{tagId:guid}",
            async (Guid jobId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobTagRelationCommand(jobId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Create a job tag relation")
            .WithName("CreateJobTagRelation");
        
        group.MapDelete("/{jobId:guid}", async (Guid jobId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobCommand(jobId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a job")
            .WithName("DeleteJob");
        
        group.MapDelete("/{jobId:guid}/tags/{tagId:guid}",
            async (Guid jobId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobTagRelationCommand(jobId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a job tag relation")
            .WithName("DeleteJobTagRelation");
        
        group.MapDelete("/tags/{tagId:guid}", async (Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobTagCommand(tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a job tag")
            .WithName("DeleteJobTag");
    }
}