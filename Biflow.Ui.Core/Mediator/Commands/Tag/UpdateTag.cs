namespace Biflow.Ui.Core;

public record UpdateTagCommand(Tag Tag) : IRequest;

[UsedImplicitly]
internal class UpdateTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateTagCommand>
{
    public async Task Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Tag).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}