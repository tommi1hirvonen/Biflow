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
            async ([FromBody] JobDto request, IMediator mediator, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobCommand(
                    JobName: request.JobName,
                    JobDescription: request.JobDescription,
                    ExecutionMode: request.ExecutionMode,
                    StopOnFirstError: request.StopOnFirstError,
                    MaxParallelSteps: request.MaxParallelSteps,
                    OvertimeNotificationLimitMinutes: request.OvertimeNotificationLimitMinutes,
                    TimeoutMinutes: request.TimeoutMinutes,
                    IsEnabled: request.IsEnabled,
                    IsPinned: request.IsPinned);
                var job = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJob", new { jobId = job.JobId });
                return Results.Created(url, job);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<Job>(StatusCodes.Status201Created)
            .WithDescription("Create a new job")
            .WithName("CreateJob");
        
        group.MapPost("/tags",
            async ([FromBody] TagDto tagDto, IMediator mediator, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobTagCommand(
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJobTag", new { tagId = tag.TagId });
                return Results.Created(url, tag);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<JobTag>(StatusCodes.Status201Created)
            .WithDescription("Create a new job tag")
            .WithName("CreateJobTag");
        
        group.MapPut("/{jobId:guid}",
            async (Guid jobId, [FromBody] JobDto request, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobCommand(
                    JobId: jobId,
                    JobName: request.JobName,
                    JobDescription: request.JobDescription,
                    ExecutionMode: request.ExecutionMode,
                    StopOnFirstError: request.StopOnFirstError,
                    MaxParallelSteps: request.MaxParallelSteps,
                    OvertimeNotificationLimitMinutes: request.OvertimeNotificationLimitMinutes,
                    TimeoutMinutes: request.TimeoutMinutes,
                    IsEnabled: request.IsEnabled,
                    IsPinned: request.IsPinned);
                var job = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Job>()
            .WithDescription("Update an existing job")
            .WithName("UpdateJob");

        group.MapPut("/{jobId:guid}/concurrencies",
            async ([FromRoute] Guid jobId, [FromBody] JobConcurrencyDto[] concurrencies,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dictionary = concurrencies.ToDictionary(key => key.StepType, value => value.MaxParallelSteps);
                var command = new UpdateJobConcurrenciesCommand(jobId, dictionary);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Update job concurrencies for an existing job")
            .WithName("UpdateJobConcurrencies");

        group.MapPut("/tags/{tagId:guid}",
            async (Guid tagId, [FromBody] TagDto tagDto, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobTagCommand(
                    TagId: tagId,
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<JobTag>()
            .WithDescription("Update an existing job tag")
            .WithName("UpdateJobTag");
        
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