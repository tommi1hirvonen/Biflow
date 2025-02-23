namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="QlikCloudEnvironmentId"></param>
/// <param name="QlikCloudEnvironmentName"></param>
/// <param name="EnvironmentUrl"></param>
/// <param name="ApiToken">Pass null to retain the previous ApiToken value</param>
public record UpdateQlikCloudEnvironmentCommand(
    Guid QlikCloudEnvironmentId,
    string QlikCloudEnvironmentName,
    string EnvironmentUrl,
    string? ApiToken) : IRequest<QlikCloudEnvironment>;

[UsedImplicitly]
internal class UpdateQlikCloudEnvironmentCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateQlikCloudEnvironmentCommand, QlikCloudEnvironment>
{
    public async Task<QlikCloudEnvironment> Handle(UpdateQlikCloudEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var environment = await dbContext.QlikCloudEnvironments
            .FirstOrDefaultAsync(x => x.QlikCloudEnvironmentId == request.QlikCloudEnvironmentId, cancellationToken)
            ?? throw new NotFoundException<QlikCloudEnvironment>(request.QlikCloudEnvironmentId);
        environment.QlikCloudEnvironmentName = request.QlikCloudEnvironmentName;
        environment.EnvironmentUrl = request.EnvironmentUrl;
        if (request.ApiToken is not null)
        {
            environment.ApiToken = request.ApiToken;
        }
        environment.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return environment;
    }
}