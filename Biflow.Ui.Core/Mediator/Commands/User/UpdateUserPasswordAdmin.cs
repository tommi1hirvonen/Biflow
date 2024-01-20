using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

/// <summary>
/// Update the password for an existing user. Should only be used when the authentication mode is BuiltIn.
/// </summary>
/// <param name="Username">Username for the account</param>
/// <param name="Password">New password</param>
public record UpdateUserPasswordAdminCommand(string Username, string Password) : IRequest;

internal class UpdateUserPasswordAdminCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserPasswordAdminCommand>
{
    public async Task Handle(UpdateUserPasswordAdminCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var newHash = BC.HashPassword(request.Password);
        var affectedRows = await context.Users
            .Where(u => u.Username == request.Username)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => EF.Property<string?>(u, "PasswordHash"), newHash), cancellationToken);
        if (affectedRows == 0)
        {
            throw new ArgumentException($"No user found with username {request.Username}");
        }
    }
}