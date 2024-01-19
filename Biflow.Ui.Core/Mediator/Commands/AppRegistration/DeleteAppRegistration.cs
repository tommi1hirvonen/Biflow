namespace Biflow.Ui.Core;

public record DeleteAppRegistrationCommand(Guid AppRegistrationId) : IRequest;

internal class DeleteAppRegistrationCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteAppRegistrationCommand>
{
    public async Task Handle(DeleteAppRegistrationCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.AppRegistrations
            .FirstOrDefaultAsync(p => p.AppRegistrationId == request.AppRegistrationId, cancellationToken);
        if (client is not null)
        {
            context.AppRegistrations.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}