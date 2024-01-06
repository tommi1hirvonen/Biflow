using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

public record UserAuthenticateQuery(string Username, string Password) : IRequest<UserAuthenticateQueryResponse>;

public record UserAuthenticateQueryResponse(IEnumerable<string> Roles);

internal class UserAuthenticateQueryHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UserAuthenticateQuery, UserAuthenticateQueryResponse>
{
    public async Task<UserAuthenticateQueryResponse> Handle(UserAuthenticateQuery request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var result = await context.Users
            .Where(u => u.Username == request.Username)
            .Select(u => new
            {
                PasswordHash = EF.Property<string>(u, "PasswordHash"),
                u.Roles
            })
            .FirstOrDefaultAsync(cancellationToken);
        var (hash, roles) = (result?.PasswordHash, result?.Roles);
        if (hash is null || roles is null)
        {
            return new([]);
        }

        var auth = BC.Verify(request.Password, hash);
        if (!auth)
        {
            return new([]);
        }

        return new(roles ?? []);
    }
}