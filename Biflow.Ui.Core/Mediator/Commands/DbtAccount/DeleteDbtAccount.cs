namespace Biflow.Ui.Core;

public record DeleteDbtAccountCommand(Guid DbtAccountId) : IRequest;

internal class DeleteDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDbtAccountCommand>
{
    public async Task Handle(DeleteDbtAccountCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var account = await context.DbtAccounts
            .FirstOrDefaultAsync(a => a.DbtAccountId == request.DbtAccountId, cancellationToken);
        if (account is not null)
        {
            context.DbtAccounts.Remove(account);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
