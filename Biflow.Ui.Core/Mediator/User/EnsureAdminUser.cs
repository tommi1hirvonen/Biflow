using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

/// <summary>
/// Ensures there is a user with Admin role added to the system database.
/// </summary>
/// <param name="Username">Username for the admin user</param>
/// <param name="Password">Password for the admin user.
/// <see langword="null"/> can be passed when using an authentication method other than built-in.</param>
public record EnsureAdminUserCommand(string Username, string? Password = null) : IRequest;

internal class EnsureAdminUserCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<EnsureAdminUserCommand>
{
    public async Task Handle(EnsureAdminUserCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = new User
        {
            Username = request.Username,
            CreatedOn = DateTimeOffset.Now,
            LastModifiedOn = DateTimeOffset.Now,
            Roles = [Roles.Admin]
        };

        var passwordHash = string.IsNullOrEmpty(request.Password)
            ? null
            : BC.HashPassword(request.Password);

        var affectedRows = await context.Users
            .Where(u => u.Username == request.Username)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => u.Roles, user.Roles)
                .SetProperty(u => EF.Property<string?>(u, "PasswordHash"), passwordHash), cancellationToken);
        
        if (affectedRows == 0)
        {
            context.Users.Add(user);
            context.Entry(user)
                .Property("PasswordHash")
                .CurrentValue = passwordHash;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
