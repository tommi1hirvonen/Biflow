namespace Biflow.Ui.Core;

public record CreateDbtAccountCommand(DbtAccount Account) : IRequest;

internal class CreateDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDbtAccountCommand>
{
    public async Task Handle(CreateDbtAccountCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.DbtAccounts.Add(request.Account);
        await context.SaveChangesAsync(cancellationToken);
    }
}
