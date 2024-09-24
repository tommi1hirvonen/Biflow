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
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var result = await context.Users
            .Where(user => user.Username == request.Username)
            .Select(user => new
            {
                PasswordHash = EF.Property<string>(user, "PasswordHash"),
                User = user
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

        // No last login or it was over an hour ago.
        if (user.LastLoginOn is null || user.LastLoginOn is DateTimeOffset dto && dto.AddHours(1) < DateTimeOffset.UtcNow)
        {
            // Update last login date and time.
            user.LastLoginOn = DateTimeOffset.UtcNow;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user last login date and time.");
            }
        }

        return new(user.Roles ?? []);
    }
}