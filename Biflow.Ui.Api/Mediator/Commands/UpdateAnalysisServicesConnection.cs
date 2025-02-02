namespace Biflow.Ui.Api.Mediator.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="ConnectionId"></param>
/// <param name="ConnectionName"></param>
/// <param name="ConnectionString">Pass null to retain the previous ConnectionString value</param>
/// <param name="CredentialId"></param>
public record UpdateAnalysisServicesConnectionCommand(
    Guid ConnectionId,
    string ConnectionName,
    string? ConnectionString,
    Guid? CredentialId) : IRequest<AnalysisServicesConnection>;

[UsedImplicitly]
internal class UpdateAnalysisServicesConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateAnalysisServicesConnectionCommand, AnalysisServicesConnection>
{
    public async Task<AnalysisServicesConnection> Handle(UpdateAnalysisServicesConnectionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var connection = await dbContext.AnalysisServicesConnections
            .FirstOrDefaultAsync(x => x.ConnectionId == request.ConnectionId, cancellationToken)
            ?? throw new NotFoundException<AnalysisServicesConnection>(request.ConnectionId);
        
        if (request.CredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(c => c.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }
        
        connection.ConnectionName = request.ConnectionName;
        connection.CredentialId = request.CredentialId;
        if (request.ConnectionString is not null)
        {
            connection.ConnectionString = request.ConnectionString;
        }
        
        connection.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return connection;
    }
}