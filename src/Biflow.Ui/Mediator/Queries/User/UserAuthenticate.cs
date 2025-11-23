namespace Biflow.Ui.Mediator.Queries.User;

public record UserAuthenticateQuery(string Username, string Password) : IRequest<UserAuthenticateQueryResponse>;

public record UserAuthenticateQueryResponse(IReadOnlyList<string> Roles);

internal class UserAuthenticateQueryHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<UserAuthenticateQueryHandler> logger)
    : IRequestHandler<UserAuthenticateQuery, UserAuthenticateQueryResponse>
{
    public async Task<UserAuthenticateQueryResponse> Handle(UserAuthenticateQuery request,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var result = await context.Users
            .Where(u => u.Username == request.Username)
            .Select(u => new
            {
                PasswordHash = EF.Property<string>(u, "PasswordHash"),
                User = u
            })
            .FirstOrDefaultAsync(cancellationToken);
        var (hash, user) = (result?.PasswordHash, result?.User);

        if (hash is null || user is null)
        {
            return new UserAuthenticateQueryResponse([]);
        }

        var auth = PasswordHasher.Verify(hash, request.Password);
        if (!auth)
        {
            return new UserAuthenticateQueryResponse([]);
        }
        
        // The last login was under an hour ago.
        if (user.LastLoginOn is { } lastLogin && DateTime.UtcNow - lastLogin < TimeSpan.FromHours(1))
        {
            return new UserAuthenticateQueryResponse(user.Roles);
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

        return new UserAuthenticateQueryResponse(user.Roles);
    }
}