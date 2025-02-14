namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class UsersWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.UsersWrite]);
        
        var group = app.MapGroup("/users")
            .WithTags(Scopes.UsersWrite)
            .AddEndpointFilter(endpointFilter);

        group.MapDelete("/{userId:guid}",
            async (Guid userId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteUserCommand(userId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete user")
            .WithDescription("Delete user")
            .WithName("DeleteUser");
    }
}