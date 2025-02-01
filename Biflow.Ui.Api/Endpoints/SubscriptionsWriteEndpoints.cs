using Biflow.Ui.Api.Mediator.Commands;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
internal abstract class SubscriptionsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter =
            apiKeyEndpointFilterFactory.Create([Scopes.SubscriptionsWrite]);
        
        var group = app.MapGroup("/subscriptions")
            .WithTags(Scopes.SubscriptionsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("/job", async (CreateJobSubscription subscriptionDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobSubscriptionCommand(
                    subscriptionDto.UserId,
                    subscriptionDto.JobId,
                    subscriptionDto.AlertType,
                    subscriptionDto.NotifyOnOvertime);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSubscription",
                    new { subscriptionId = subscription.SubscriptionId });
                return Results.Created(url, subscription);
            })
            .ProducesValidationProblem()
            .Produces<JobSubscription>()
            .WithSummary("Create job subscription")
            .WithDescription("Create a new job subscription")
            .WithName("CreateJobSubscription");
        
        group.MapPost("/step", async (CreateStepSubscription subscriptionDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateStepSubscriptionCommand(
                    subscriptionDto.UserId,
                    subscriptionDto.StepId,
                    subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSubscription",
                    new { subscriptionId = subscription.SubscriptionId });
                return Results.Created(url, subscription);
            })
            .ProducesValidationProblem()
            .Produces<StepSubscription>()
            .WithSummary("Create step subscription")
            .WithDescription("Create a new step subscription")
            .WithName("CreateStepSubscription");
        
        group.MapPost("/steptag", async (CreateStepTagSubscription subscriptionDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateStepTagSubscriptionCommand(
                    subscriptionDto.UserId,
                    subscriptionDto.StepTagId,
                    subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSubscription",
                    new { subscriptionId = subscription.SubscriptionId });
                return Results.Created(url, subscription);
            })
            .ProducesValidationProblem()
            .Produces<TagSubscription>()
            .WithSummary("Create step tag subscription")
            .WithDescription("Create a new step tag subscription")
            .WithName("CreateStepTagSubscription");
        
        group.MapPost("/jobsteptag", async (CreateJobStepTagSubscription subscriptionDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobStepTagSubscriptionCommand(
                    subscriptionDto.UserId,
                    subscriptionDto.JobId,
                    subscriptionDto.StepTagId,
                    subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSubscription",
                    new { subscriptionId = subscription.SubscriptionId });
                return Results.Created(url, subscription);
            })
            .ProducesValidationProblem()
            .Produces<TagSubscription>()
            .WithSummary("Create job-step tag subscription")
            .WithDescription("Create a new job-step tag subscription")
            .WithName("CreateJobStepTagSubscription");
        
        group.MapPut("/job/{subscriptionId:guid}", async (Guid subscriptionId, UpdateJobSubscription subscriptionDto, 
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobSubscriptionCommand(
                    subscriptionId,
                    subscriptionDto.AlertType,
                    subscriptionDto.NotifyOnOvertime);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(subscription);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<JobSubscription>()
            .WithSummary("Update job subscription")
            .WithDescription("Update an existing job subscription")
            .WithName("UpdateJobSubscription");
        
        group.MapPut("/step/{subscriptionId:guid}", async (Guid subscriptionId, UpdateStepSubscription subscriptionDto, 
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateStepSubscriptionCommand(subscriptionId, subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(subscription);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<StepSubscription>()
            .WithSummary("Update step subscription")
            .WithDescription("Update an existing step subscription")
            .WithName("UpdateStepSubscription");
        
        group.MapPut("/steptag/{subscriptionId:guid}", async (
                Guid subscriptionId,
                UpdateStepTagSubscription subscriptionDto,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateStepTagSubscriptionCommand(
                    subscriptionId,
                    subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(subscription);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<TagSubscription>()
            .WithSummary("Update step tag subscription")
            .WithDescription("Update step tag subscription")
            .WithName("UpdateStepTagSubscription");
        
        group.MapPut("/jobsteptag/{subscriptionId:guid}", async (
                Guid subscriptionId,
                UpdateJobStepTagSubscription subscriptionDto,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobStepTagSubscriptionCommand(
                    subscriptionId,
                    subscriptionDto.AlertType);
                var subscription = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(subscription);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<TagSubscription>()
            .WithSummary("Update job-step tag subscription")
            .WithDescription("Update job-step tag subscription")
            .WithName("UpdateJobStepTagSubscription");
        
        group.MapDelete("/{subscriptionId:guid}", async (Guid subscriptionId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteSubscriptionCommand(subscriptionId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete subscription")
            .WithDescription("Delete subscription")
            .WithName("DeleteSubscription");
    }
}