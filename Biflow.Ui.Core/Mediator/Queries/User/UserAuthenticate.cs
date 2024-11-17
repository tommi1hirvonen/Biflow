using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

public record UserAuthenticateQuery(string Username, string Password) : IRequest<UserAuthenticateQueryResponse>;

public record UserAuthenticateQueryResponse(IEnumerable<string> Roles);

internal class UserAuthenticateQueryHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<UserAuthenticateQueryHandler> logger)
    : IRequestHandler<UserAuthenticateQuery, UserAuthenticateQueryResponse>
{
    public async Task<UserAuthenticateQueryResponse> Handle(UserAuthenticateQuery request, CancellationToken cancellationToken)
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
            return new([]);
        }

        var auth = BC.Verify(request.Password, hash);
        if (!auth)
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