namespace Biflow.Ui;

public record UserRolesQuery(string Username) : IRequest<UserRolesQueryResponse>;

public record UserRolesQueryResponse(IEnumerable<string> Roles);

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
            return new([]);
        }

        // Last login was under an hour ago.
        if (user.LastLoginOn is { } lastLogin && DateTime.UtcNow - lastLogin < TimeSpan.FromHours(1))
        {
            return new(user.Roles);
        }
        
        // Update last login date and time.
        try
        {
            user.LastLoginOn = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user last login date and time.");
        }

        return new(user.Roles);
    }
}