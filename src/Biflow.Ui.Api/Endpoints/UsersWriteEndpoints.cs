using Biflow.Ui.Api.Mediator.Commands;

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

        group.MapPost("", async (CreateUser dto, IMediator mediator,
            LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var role = dto.MainRole switch
                {
                    Models.UserRole.Admin => Core.UserRole.Admin,
                    Models.UserRole.Editor => Core.UserRole.Editor,
                    Models.UserRole.Operator => Core.UserRole.Operator,
                    Models.UserRole.Viewer => Core.UserRole.Viewer,
                    _ => throw new ArgumentOutOfRangeException($"Unrecognized user role {dto.MainRole}")
                };
                var command = new CreateUserCommand(
                    Username: dto.Username, 
                    Email: dto.Email, 
                    AuthorizeAllJobs: dto.AuthorizeAllJobs, 
                    AuthorizeAllDataTables: dto.AuthorizeAllDataTables, 
                    AuthorizedJobIds: dto.AuthorizedJobIds, 
                    AuthorizedDataTableIds: dto.AuthorizedDataTableIds, 
                    MainRole: role, 
                    IsSettingsEditor: dto.IsSettingsEditor, 
                    IsDataTableMaintainer: dto.IsDataTableMaintainer, 
                    IsVersionManager: dto.IsVersionManager, 
                    Password: dto.Password);
                var user = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetUser", new { userId = user.UserId });
                return Results.Created(url, user);
            })
            .ProducesValidationProblem()
            .Produces<User>()
            .WithSummary("Create user")
            .WithDescription("Create a new user")
            .WithName("CreateUser");
        
        group.MapPut("/{userId:guid}", async (Guid userId, UpdateUser dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var role = dto.MainRole switch
                {
                    Models.UserRole.Admin => Core.UserRole.Admin,
                    Models.UserRole.Editor => Core.UserRole.Editor,
                    Models.UserRole.Operator => Core.UserRole.Operator,
                    Models.UserRole.Viewer => Core.UserRole.Viewer,
                    _ => throw new ArgumentOutOfRangeException($"Unrecognized user role {dto.MainRole}")
                };
                var command = new UpdateUserCommand(
                    UserId: userId,
                    Username: dto.Username, 
                    Email: dto.Email, 
                    AuthorizeAllJobs: dto.AuthorizeAllJobs, 
                    AuthorizeAllDataTables: dto.AuthorizeAllDataTables, 
                    AuthorizedJobIds: dto.AuthorizedJobIds, 
                    AuthorizedDataTableIds: dto.AuthorizedDataTableIds, 
                    MainRole: role, 
                    IsSettingsEditor: dto.IsSettingsEditor, 
                    IsDataTableMaintainer: dto.IsDataTableMaintainer, 
                    IsVersionManager: dto.IsVersionManager);
                var user = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(user);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<User>()
            .WithSummary("Update user")
            .WithDescription("Update an existing user")
            .WithName("UpdateUser");
        
        group.MapPatch("/{userId:guid}/password",
            async (Guid userId, PasswordDto dto, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateUserPasswordAdminCommand(userId, dto.Password);
                await mediator.SendAsync(command, cancellationToken);
                return Results.Ok();
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Reset user's password")
            .WithDescription("Reset user's password")
            .WithName("ResetPassword");

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