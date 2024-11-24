namespace Biflow.Ui.Core;

public record UpdateDbtAccountCommand(DbtAccount Account) : IRequest;

internal class UpdateDbtAccountCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDbtAccountCommand>
{
    public async Task Handle(UpdateDbtAccountCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Account).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
