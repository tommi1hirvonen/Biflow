namespace Biflow.Ui.Core;

public record CreateMsSqlConnectionCommand(
    string ConnectionName,
    int MaxConcurrentSqlSteps,
    int MaxConcurrentPackageSteps,
    string? ExecutePackagesAsLogin,
    Guid? CredentialId,
    string? ScdDefaultTargetSchema,
    string? ScdDefaultTargetTableSuffix,
    string? ScdDefaultStagingSchema,
    string? ScdDefaultStagingTableSuffix,
    string ConnectionString) : IRequest<MsSqlConnection>;

[UsedImplicitly]
internal class CreateMsSqlConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateMsSqlConnectionCommand, MsSqlConnection>
{
    public async Task<MsSqlConnection> Handle(CreateMsSqlConnectionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (request.CredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(x => x.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }

        var connection = new MsSqlConnection
        {
            ConnectionName = request.ConnectionName,
            MaxConcurrentSqlSteps = request.MaxConcurrentSqlSteps,
            MaxConcurrentPackageSteps = request.MaxConcurrentPackageSteps,
            ExecutePackagesAsLogin = request.ExecutePackagesAsLogin,
            CredentialId = request.CredentialId,
            ConnectionString = request.ConnectionString,
            ScdDefaultTargetSchema = request.ScdDefaultTargetSchema,
            ScdDefaultStagingSchema = request.ScdDefaultStagingSchema
        };

        if (request.ScdDefaultTargetTableSuffix is not null)
        {
            connection.ScdDefaultTargetTableSuffix = request.ScdDefaultTargetTableSuffix;
        }

        if (request.ScdDefaultStagingTableSuffix is not null)
        {
            connection.ScdDefaultStagingTableSuffix = request.ScdDefaultStagingTableSuffix;
        }
        
        connection.EnsureDataAnnotationsValidated();
        
        dbContext.MsSqlConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return connection;
    }
}