namespace Biflow.Ui.Core;

public record UpdateConnectionCommand(ConnectionInfoBase Connection) : IRequest;

internal class UpdateConnectionCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory) : IRequestHandler<UpdateConnectionCommand>
{
    public async Task Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Connection).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}