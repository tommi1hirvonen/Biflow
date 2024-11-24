namespace Biflow.Ui.Core;

public record UpdateQlikCloudEnvironmentCommand(QlikCloudEnvironment Environment) : IRequest;

internal class UpdateQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateQlikCloudEnvironmentCommand>
{
    public async Task Handle(UpdateQlikCloudEnvironmentCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Environment).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}