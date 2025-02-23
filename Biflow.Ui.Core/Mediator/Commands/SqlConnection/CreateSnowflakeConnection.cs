namespace Biflow.Ui.Core;

public record CreateSnowflakeConnectionCommand(
    string ConnectionName,
    int MaxConcurrentSqlSteps,
    string? ScdDefaultTargetSchema,
    string? ScdDefaultTargetTableSuffix,
    string? ScdDefaultStagingSchema,
    string? ScdDefaultStagingTableSuffix,
    string ConnectionString) : IRequest<SnowflakeConnection>;

[UsedImplicitly]
internal class CreateSnowflakeConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateSnowflakeConnectionCommand, SnowflakeConnection>
{
    public async Task<SnowflakeConnection> Handle(CreateSnowflakeConnectionCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var connection = new SnowflakeConnection
        {
            ConnectionName = request.ConnectionName,
            MaxConcurrentSqlSteps = request.MaxConcurrentSqlSteps,
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
        
        dbContext.SnowflakeConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return connection;
    }
}