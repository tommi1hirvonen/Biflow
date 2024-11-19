namespace Biflow.Ui.Core;

public record DeleteQlikCloudEnvironmentCommand(Guid QlikCloudEnvironmentId) : IRequest;

internal class DeleteQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteQlikCloudEnvironmentCommand>
{
    public async Task Handle(DeleteQlikCloudEnvironmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var environment = await context.QlikCloudEnvironments
            .FirstOrDefaultAsync(c => c.QlikCloudEnvironmentId == request.QlikCloudEnvironmentId, cancellationToken);
        if (environment is not null)
        {
            context.QlikCloudEnvironments.Remove(environment);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}