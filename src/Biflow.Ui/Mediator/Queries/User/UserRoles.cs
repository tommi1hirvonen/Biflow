using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Queries.User;

public record UserRolesQuery(string Username) : IRequest<UserRolesQueryResponse>;

public record UserRolesQueryResponse(IReadOnlyList<string> Roles);

[UsedImplicitly]
internal class UserRolesQueryHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<UserRolesQueryHandler> logger)
    : IRequestHandler<UserRolesQuery, UserRolesQueryResponse>
{
    public async Task<UserRolesQueryResponse> Handle(UserRolesQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users
            .Where(u => u.Username == request.Username)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return new UserRolesQueryResponse([]);
        }

        // The last login was under an hour ago.
        if (user.LastLoginOn is { } lastLogin && DateTime.UtcNow - lastLogin < TimeSpan.FromHours(1))
        {
            return new UserRolesQueryResponse(user.Roles);
        }
        
        // Update the last login date and time.
        try
        {
            user.LastLoginOn = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user last login date and time.");
        }

        return new UserRolesQueryResponse(user.Roles);
    }
}