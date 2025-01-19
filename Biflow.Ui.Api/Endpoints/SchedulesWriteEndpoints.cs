using Biflow.Ui.Api.Mediator.Commands;

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
    }
}