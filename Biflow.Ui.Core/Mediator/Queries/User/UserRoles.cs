using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

public record UserRolesQuery(string Username) : IRequest<UserRolesQueryResponse>;

public record UserRolesQueryResponse(IEnumerable<string> Roles);

internal class UserRolesQueryHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<UserRolesQueryHandler> logger)
    : IRequestHandler<UserRolesQuery, UserRolesQueryResponse>
{
    public async Task<UserRolesQueryResponse> Handle(UserRolesQuery request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users
            .Where(u => u.Username == request.Username)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
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

        return new(user.Roles);
    }
}