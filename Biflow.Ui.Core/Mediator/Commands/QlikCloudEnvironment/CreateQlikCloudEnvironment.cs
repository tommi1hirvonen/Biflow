namespace Biflow.Ui.Core;

public record CreateQlikCloudEnvironmentCommand(QlikCloudEnvironment Environment) : IRequest;

internal class CreateQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateQlikCloudEnvironmentCommand>
{
    public async Task Handle(CreateQlikCloudEnvironmentCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.QlikCloudEnvironments.Add(request.Environment);
        await context.SaveChangesAsync(cancellationToken);
    }
}