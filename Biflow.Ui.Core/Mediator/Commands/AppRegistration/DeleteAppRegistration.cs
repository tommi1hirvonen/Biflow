namespace Biflow.Ui.Core;

public record DeleteAppRegistrationCommand(Guid AppRegistrationId) : IRequest;

internal class DeleteAppRegistrationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteAppRegistrationCommand>
{
    public async Task Handle(DeleteAppRegistrationCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.AppRegistrations
            .FirstOrDefaultAsync(p => p.AppRegistrationId == request.AppRegistrationId, cancellationToken);
        if (client is not null)
        {
            context.AppRegistrations.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}