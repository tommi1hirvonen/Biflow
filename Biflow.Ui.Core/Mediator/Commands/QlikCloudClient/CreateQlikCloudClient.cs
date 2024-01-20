namespace Biflow.Ui.Core;

public record CreateQlikCloudClientCommand(QlikCloudClient Client) : IRequest;

internal class CreateQlikCloudClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateQlikCloudClientCommand>
{
    public async Task Handle(CreateQlikCloudClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.QlikCloudClients.Add(request.Client);
        await context.SaveChangesAsync(cancellationToken);
    }
}