namespace Biflow.Ui.Core;

public record UpdateTagCommand(Tag Tag) : IRequest;

internal class UpdateTagCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory) : IRequestHandler<UpdateTagCommand>
{
    public async Task Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Tag).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}