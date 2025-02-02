namespace Biflow.Ui.Api.Mediator.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="AzureCredentialName"></param>
/// <param name="ClientId">Pass null to use system-assigned managed identity</param>
public record UpdateManagedIdentityAzureCredentialCommand(
    Guid AzureCredentialId,
    string AzureCredentialName,
    string? ClientId) : IRequest<ManagedIdentityAzureCredential>;

[UsedImplicitly]
internal class UpdateManagedIdentityAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateManagedIdentityAzureCredentialCommand, ManagedIdentityAzureCredential>
{
    public async Task<ManagedIdentityAzureCredential> Handle(UpdateManagedIdentityAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = await dbContext.ManagedIdentityCredentials
            .FirstOrDefaultAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken)
            ?? throw new NotFoundException<ManagedIdentityAzureCredential>(request.AzureCredentialId);
        
        credential.AzureCredentialName = request.AzureCredentialName;
        credential.ClientId = request.ClientId;
        
        credential.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}