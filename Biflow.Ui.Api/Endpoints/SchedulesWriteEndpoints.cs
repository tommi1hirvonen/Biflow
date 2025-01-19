using Biflow.Ui.Api.Mediator.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class SchedulesWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.SchedulesWrite]);

        app.MapPost("/jobs/{jobId:guid}/schedules", async (Guid jobId, ScheduleDto scheduleDto, IMediator mediator,
            LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateScheduleCommand(
                    JobId: jobId,
                    ScheduleName: scheduleDto.ScheduleName,
                    CronExpression: scheduleDto.CronExpression,
                    IsEnabled: scheduleDto.IsEnabled,
                    DisallowConcurrentExecution: scheduleDto.DisallowConcurrentExecution,
                    ScheduleTagIds: scheduleDto.ScheduleTagIds,
                    FilterStepTagIds: scheduleDto.FilterStepTagIds);
                var schedule = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSchedule", new { scheduleId = schedule.ScheduleId });
                return Results.Created(url, schedule);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Schedule>()
            .WithDescription("Create a new schedule")
            .WithName("CreateSchedule")
            .WithTags(Scopes.SchedulesWrite)
            .AddEndpointFilter(endpointFilter);
        
        var group = app.MapGroup("/jobs/schedules")
            .WithTags(Scopes.SchedulesWrite)
            .AddEndpointFilter(endpointFilter);

        group.MapPut("/{scheduleId:guid}", async (Guid scheduleId, ScheduleDto scheduleDto, IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var command = new UpdateScheduleCommand(
                    ScheduleId: scheduleId,
                    ScheduleName: scheduleDto.ScheduleName,
                    CronExpression: scheduleDto.CronExpression,
                    IsEnabled: scheduleDto.IsEnabled,
                    DisallowConcurrentExecution: scheduleDto.DisallowConcurrentExecution,
                    ScheduleTagIds: scheduleDto.ScheduleTagIds,
                    FilterStepTagIds: scheduleDto.FilterStepTagIds);
                var schedule = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(schedule);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Schedule>()
            .WithDescription("Update an existing schedule")
            .WithName("UpdateSchedule");
        
        group.MapDelete("/{scheduleId:guid}", async (Guid scheduleId, IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var command = new DeleteScheduleCommand(scheduleId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete an existing schedule")
            .WithName("DeleteSchedule");
        
        group.MapPost("/tags",
            async ([FromBody] TagDto tagDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateScheduleTagCommand(
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetScheduleTag", new { tagId = tag.TagId });
                return Results.Created(url, tag);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<ScheduleTag>(StatusCodes.Status201Created)
            .WithDescription("Create a new schedule tag")
            .WithName("CreateScheduleTag");

        group.MapPut("/tags/{tagId:guid}",
            async (Guid tagId, [FromBody] TagDto tagDto, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateScheduleTagCommand(
                    TagId: tagId,
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<ScheduleTag>()
            .WithDescription("Update an existing schedule tag")
            .WithName("UpdateScheduleTag");
        
        group.MapDelete("/tags/{tagId:guid}", async (Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteScheduleTagCommand(tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a schedule tag")
            .WithName("DeleteScheduleTag");
    }
}