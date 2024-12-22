using Biflow.Core.Constants;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

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
            .WithDescription("Get all users")
            .WithName("GetUsers");
        
        group.MapGet("/{userId:guid}",
            async (ServiceDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                return user is null ? Results.NotFound() : Results.Ok(user);
            })
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
                return user is null ? Results.NotFound() : Results.Ok(user.Jobs);
            })
            .WithTags($"{Scopes.UsersRead}, {Scopes.JobsRead}")
            .WithDescription("Get user's authorized jobs. " +
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
                return user is null ? Results.NotFound() : Results.Ok(user.DataTables);
            })
            .WithTags($"{Scopes.UsersRead}, {Scopes.DataTablesRead}")
            .WithDescription("Get user's authorized data tables. " +
                             "The collection properties of the returned tables are not loaded and will be empty. " +
                             "If the user property AuthorizeAllDataTables is set to true, " +
                             "the list of authorized data tables has no effect.")
            .WithName("GetUserDataTables")
            .AddEndpointFilter(userDataTablesEndpointFilter);
    }
}