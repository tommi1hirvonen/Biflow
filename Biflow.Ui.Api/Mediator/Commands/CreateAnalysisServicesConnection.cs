namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateAnalysisServicesConnectionCommand(
    string ConnectionName,
    string ConnectionString,
    Guid? CredentialId) : IRequest<AnalysisServicesConnection>;

[UsedImplicitly]
internal class CreateAnalysisServicesConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateAnalysisServicesConnectionCommand, AnalysisServicesConnection>
{
    public async Task<AnalysisServicesConnection> Handle(CreateAnalysisServicesConnectionCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        if (request.CredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(c => c.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }
        
        var connection = new AnalysisServicesConnection
        {
            ConnectionName = request.ConnectionName,
            ConnectionString = request.ConnectionString,
            CredentialId = request.CredentialId
        };
        
        connection.EnsureDataAnnotationsValidated();
        
        dbContext.AnalysisServicesConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return connection;
    }
}