namespace Biflow.Ui.Api.Mediator.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="ConnectionId"></param>
/// <param name="ConnectionName"></param>
/// <param name="MaxConcurrentSqlSteps"></param>
/// <param name="ScdDefaultTargetSchema"></param>
/// <param name="ScdDefaultTargetTableSuffix"></param>
/// <param name="ScdDefaultStagingSchema"></param>
/// <param name="ScdDefaultStagingTableSuffix"></param>
/// <param name="ConnectionString">Pass null to retain the previous ConnectionString value</param>
public record UpdateSnowflakeConnectionCommand(
    Guid ConnectionId,
    string ConnectionName,
    int MaxConcurrentSqlSteps,
    string? ScdDefaultTargetSchema,
    string? ScdDefaultTargetTableSuffix,
    string? ScdDefaultStagingSchema,
    string? ScdDefaultStagingTableSuffix,
    string? ConnectionString) : IRequest<SnowflakeConnection>;

[UsedImplicitly]
internal class UpdateSnowflakeConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateSnowflakeConnectionCommand, SnowflakeConnection>
{
    public async Task<SnowflakeConnection> Handle(UpdateSnowflakeConnectionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var connection = await dbContext.SnowflakeConnections
            .FirstOrDefaultAsync(x => x.ConnectionId == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException<SnowflakeConnection>(request.ConnectionId);
        
        connection.ConnectionName = request.ConnectionName;
        connection.MaxConcurrentSqlSteps = request.MaxConcurrentSqlSteps;
        connection.ScdDefaultTargetSchema = request.ScdDefaultTargetSchema;
        connection.ScdDefaultStagingSchema = request.ScdDefaultStagingSchema;

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