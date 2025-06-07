namespace Biflow.Ui.Core;

public record DeleteQlikCloudEnvironmentCommand(Guid QlikCloudEnvironmentId) : IRequest;

[UsedImplicitly]
internal class DeleteQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteQlikCloudEnvironmentCommand>
{
    public async Task Handle(DeleteQlikCloudEnvironmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var environment = await context.QlikCloudEnvironments
            .FirstOrDefaultAsync(c => c.QlikCloudEnvironmentId == request.QlikCloudEnvironmentId, cancellationToken)
            ?? throw new NotFoundException<QlikCloudEnvironment>(request.QlikCloudEnvironmentId);
        context.QlikCloudEnvironments.Remove(environment);
        await context.SaveChangesAsync(cancellationToken);
    }
}