namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="AzureCredentialName"></param>
/// <param name="ClientId">Pass null to use system-assigned managed identity</param>
public record CreateManagedIdentityAzureCredentialCommand(
    string AzureCredentialName,
    string? ClientId) : IRequest<ManagedIdentityAzureCredential>;

[UsedImplicitly]
internal class CreateManagedIdentityAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateManagedIdentityAzureCredentialCommand, ManagedIdentityAzureCredential>
{
    public async Task<ManagedIdentityAzureCredential> Handle(CreateManagedIdentityAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = new ManagedIdentityAzureCredential
        {
            AzureCredentialName = request.AzureCredentialName,
            ClientId = request.ClientId
        };
        
        credential.EnsureDataAnnotationsValidated();
        
        dbContext.ManagedIdentityCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}