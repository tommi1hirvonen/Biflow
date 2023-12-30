using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record ClearAppRegistrationTokensCommand(Guid AppRegistrationId) : IRequest;

internal class ClearAppRegistrationTokensCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory, ITokenService tokenService)
    : IRequestHandler<ClearAppRegistrationTokensCommand>
{
    public async Task Handle(ClearAppRegistrationTokensCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tokens = await context.AccessTokens
            .Where(t => t.AppRegistrationId == request.AppRegistrationId)
            .ToArrayAsync(cancellationToken);
        foreach (var token in tokens)
        {
            context.AccessTokens.Remove(token);
        }
        await context.SaveChangesAsync(cancellationToken);
        tokenService.Clear();
    }
}
