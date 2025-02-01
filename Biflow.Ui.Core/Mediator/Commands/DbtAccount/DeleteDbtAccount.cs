namespace Biflow.Ui.Core;

public record DeleteDbtAccountCommand(Guid DbtAccountId) : IRequest;

[UsedImplicitly]
internal class DeleteDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDbtAccountCommand>
{
    public async Task Handle(DeleteDbtAccountCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var account = await context.DbtAccounts
            .FirstOrDefaultAsync(a => a.DbtAccountId == request.DbtAccountId, cancellationToken)
            ?? throw new NotFoundException<DbtAccount>(request.DbtAccountId);
        context.DbtAccounts.Remove(account);
        await context.SaveChangesAsync(cancellationToken);
    }
}
