namespace Biflow.Ui.Api.Mediator.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="ConnectionId"></param>
/// <param name="ConnectionName"></param>
/// <param name="MaxConcurrentSqlSteps"></param>
/// <param name="MaxConcurrentPackageSteps"></param>
/// <param name="ExecutePackagesAsLogin"></param>
/// <param name="CredentialId"></param>
/// <param name="ScdDefaultTargetSchema"></param>
/// <param name="ScdDefaultTargetTableSuffix"></param>
/// <param name="ScdDefaultStagingSchema"></param>
/// <param name="ScdDefaultStagingTableSuffix"></param>
/// <param name="ConnectionString">Pass null to retain the previous ConnectionString value</param>
public record UpdateMsSqlConnectionCommand(
    Guid ConnectionId,
    string ConnectionName,
    int MaxConcurrentSqlSteps,
    int MaxConcurrentPackageSteps,
    string? ExecutePackagesAsLogin,
    Guid? CredentialId,
    string? ScdDefaultTargetSchema,
    string? ScdDefaultTargetTableSuffix,
    string? ScdDefaultStagingSchema,
    string? ScdDefaultStagingTableSuffix,
    string? ConnectionString) : IRequest<MsSqlConnection>;

[UsedImplicitly]
internal class UpdateMsSqlConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateMsSqlConnectionCommand, MsSqlConnection>
{
    public async Task<MsSqlConnection> Handle(UpdateMsSqlConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var connection = await dbContext.MsSqlConnections
            .FirstOrDefaultAsync(x => x.ConnectionId == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException<MsSqlConnection>(request.ConnectionId);

        if (request.CredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(x => x.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }
        
        connection.ConnectionName = request.ConnectionName;
        connection.MaxConcurrentSqlSteps = request.MaxConcurrentSqlSteps;
        connection.MaxConcurrentPackageSteps = request.MaxConcurrentPackageSteps;
        connection.ExecutePackagesAsLogin = request.ExecutePackagesAsLogin;
        connection.ScdDefaultTargetSchema = request.ScdDefaultTargetSchema;
        connection.ScdDefaultStagingSchema = request.ScdDefaultStagingSchema;
        connection.CredentialId = request.CredentialId;

        if (request.ScdDefaultTargetTableSuffix is not null)
        {
            connection.ScdDefaultTargetTableSuffix = request.ScdDefaultTargetTableSuffix;
        }

        if (request.ScdDefaultStagingTableSuffix is not null)
        {
            connection.ScdDefaultStagingTableSuffix = request.ScdDefaultStagingTableSuffix;
        }

        if (request.ConnectionString is not null)
        {
            connection.ConnectionString = request.ConnectionString;
        }
        
        connection.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return connection;
    }
}