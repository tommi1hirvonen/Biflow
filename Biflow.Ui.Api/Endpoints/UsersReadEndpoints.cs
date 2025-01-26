namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class UsersReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.UsersRead]);
        
        var group = app.MapGroup("/users")
            .WithTags(Scopes.UsersRead)
            .AddEndpointFilter(endpointFilter);

        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Username)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<User[]>()
            .WithSummary("Get all users")
            .WithDescription("Get all users")
            .WithName("GetUsers");
        
        group.MapGet("/{userId:guid}",
            async (ServiceDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                if (user is null)
                {
                    throw new NotFoundException<User>(userId);
                }
                return Results.Ok(user);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<User>()
            .WithSummary("Get user by id")
            .WithDescription("Get user by id")
            .WithName("GetUser");
        
        var userJobsEndpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.UsersRead, Scopes.JobsRead]);
        
        app.MapGet("/users/{userId:guid}/jobs",
            async (ServiceDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.Jobs)
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                if (user is null)
                {
                    throw new NotFoundException<User>(userId);
                }
                return Results.Ok(user.Jobs);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Job[]>()
            .WithTags($"{Scopes.UsersRead}, {Scopes.JobsRead}")
            .WithSummary("Get jobs authorized for a user")
            .WithDescription("Get jobs authorized for a user. " +
                             "The collection properties of the returned jobs are not loaded and will be empty. " +
                             "If the user's property AuthorizeAllJobs is set to tue, " +
                             "the list of authorized jobs has no effect.")
            .WithName("GetUserJobs")
            .AddEndpointFilter(userJobsEndpointFilter);
        
        var userDataTablesEndpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.UsersRead, Scopes.DataTablesRead]);
        
        app.MapGet("/users/{userId:guid}/datatables",
            async (ServiceDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.DataTables)
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                if (user is null)
                {
                    throw new NotFoundException<User>(userId);
                }
                return Results.Ok(user.DataTables);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MasterDataTable[]>()
            .WithTags($"{Scopes.UsersRead}, {Scopes.DataTablesRead}")
            .WithSummary("Get data tables authorized for a user")
            .WithDescription("Get data tables authorized for a user. " +
                             "The collection properties of the returned tables are not loaded and will be empty. " +
                             "If the user property AuthorizeAllDataTables is set to true, " +
                             "the list of authorized data tables has no effect.")
            .WithName("GetUserDataTables")
            .AddEndpointFilter(userDataTablesEndpointFilter);
        
        var userSubscriptionsEndpointFilter =
            apiKeyEndpointFilterFactory.Create([Scopes.UsersRead, Scopes.SubscriptionsRead]);
        
        app.MapGet("/users/{userId:guid}/subscriptions",
            async (ServiceDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.Subscriptions)
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                if (user is null)
                {
                    throw new NotFoundException<User>(userId);
                }
                return Results.Ok(user.Subscriptions);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Subscription[]>()
            .WithTags($"{Scopes.UsersRead}, {Scopes.SubscriptionsRead}")
            .WithSummary("Get all subscriptions for a user")
            .WithDescription("Get all subscriptions for a user")
            .WithName("GetUserSubscriptions")
            .AddEndpointFilter(userSubscriptionsEndpointFilter);
    }
}