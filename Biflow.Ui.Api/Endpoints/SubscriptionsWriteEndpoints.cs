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