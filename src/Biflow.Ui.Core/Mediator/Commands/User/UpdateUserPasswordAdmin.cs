namespace Biflow.Ui.Core;

/// <summary>
/// Update the password for an existing user. Should only be used when the authentication mode is BuiltIn.
/// </summary>
/// <param name="UserId">User id of the user</param>
/// <param name="Password">New password</param>
public record UpdateUserPasswordAdminCommand(
    Guid UserId,
    [property: ComplexPassword]
    string Password) : IRequest;

[UsedImplicitly]
internal class UpdateUserPasswordAdminCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserPasswordAdminCommand>
{
    public async Task Handle(UpdateUserPasswordAdminCommand request, CancellationToken cancellationToken)
    {
        // Ensure password meets ComplexPasswordAttribute requirements
        request.EnsureDataAnnotationsValidated();
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var newHash = PasswordHasher.Hash(request.Password);
        var affectedRows = await context.Users
            .Where(u => u.UserId == request.UserId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => EF.Property<string?>(u, "PasswordHash"), newHash), cancellationToken);
        if (affectedRows == 0)
        {
            throw new NotFoundException<User>(request.UserId);
        }
    }
}