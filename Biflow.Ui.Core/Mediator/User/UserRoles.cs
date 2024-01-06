using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UserRolesQuery(string Username) : IRequest<UserRolesQueryResponse>;

public record UserRolesQueryResponse(IEnumerable<string> Roles);

internal class UserRolesQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UserRolesQuery, UserRolesQueryResponse>
{
    public async Task<UserRolesQueryResponse> Handle(UserRolesQuery request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var roles = await context.Users
            .Where(u => u.Username == request.Username)
            .Select(u => u.Roles)
            .FirstOrDefaultAsync(cancellationToken);
        return new(roles ?? []);
    }
}