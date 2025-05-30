namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class SubscriptionsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter =
            apiKeyEndpointFilterFactory.Create([Scopes.SubscriptionsRead, Scopes.JobsRead, Scopes.UsersRead]);
        
        var group = app.MapGroup("/subscriptions")
            .WithTags($"{Scopes.SubscriptionsRead}, {Scopes.JobsRead}, {Scopes.UsersRead}")
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.Subscriptions
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => ((JobSubscription)s).Job)
                    .Include(s => ((JobStepTagSubscription)s).Job)
                    .Include(s => ((JobStepTagSubscription)s).Tag)
                    .Include(s => ((StepSubscription)s).Step)
                    .Include(s => ((StepTagSubscription)s).Tag)
                    .OrderBy(s => s.UserId)
                    .ThenBy(s => s.SubscriptionType)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<Subscription[]>()
            .WithSummary("Get all subscriptions")
            .WithDescription("Get all subscriptions")
            .WithName("GetSubscriptions");
        
        group.MapGet("/{subscriptionId:guid}",
            async (ServiceDbContext dbContext, Guid subscriptionId, CancellationToken cancellationToken) =>
            {
                var subscription = await dbContext.Subscriptions
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => ((JobSubscription)s).Job)
                    .Include(s => ((JobStepTagSubscription)s).Job)
                    .Include(s => ((JobStepTagSubscription)s).Tag)
                    .Include(s => ((StepSubscription)s).Step)
                    .Include(s => ((StepTagSubscription)s).Tag)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);
                if (subscription is null)
                {
                    throw new NotFoundException<Subscription>(subscriptionId);
                }
                return Results.Ok(subscription);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Subscription>()
            .WithSummary("Get subscription by id")
            .WithDescription("Get subscription by id")
            .WithName("GetSubscription");
    }
}