namespace Biflow.Ui.Core;

public record CreateQlikCloudEnvironmentCommand(
    string QlikCloudEnvironmentName,
    string EnvironmentUrl,
    string ApiToken) : IRequest<QlikCloudEnvironment>;

[UsedImplicitly]
internal class CreateQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateQlikCloudEnvironmentCommand, QlikCloudEnvironment>
{
    public async Task<QlikCloudEnvironment> Handle(CreateQlikCloudEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var environment = new QlikCloudEnvironment
        {
            QlikCloudEnvironmentName = request.QlikCloudEnvironmentName,
            EnvironmentUrl = request.EnvironmentUrl,
            ApiToken = request.ApiToken
        };
        environment.EnsureDataAnnotationsValidated();
        dbContext.QlikCloudEnvironments.Add(environment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return environment;
    }
}