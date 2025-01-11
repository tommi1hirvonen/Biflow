using Microsoft.AspNetCore.Mvc;
using CreateJobCommand = Biflow.Ui.Api.Mediator.Commands.CreateJobCommand;
using UpdateJobCommand = Biflow.Ui.Api.Mediator.Commands.UpdateJobCommand;

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
        
        group.MapDelete("{jobId:guid}", async (Guid jobId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobCommand(jobId);
                var job = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Job>()
            .WithDescription("Delete a job")
            .WithName("DeleteJob");
    }
}