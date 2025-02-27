namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="AzureCredentialId"></param>
/// <param name="TenantId"></param>
/// <param name="ClientId"></param>
/// <param name="ClientSecret">Pass null to retain the previous ClientSecret value</param>
public record UpdateServicePrincipalAzureCredentialCommand(
    Guid AzureCredentialId,
    string AzureCredentialName,
    string TenantId,
    string ClientId,
    string? ClientSecret) : IRequest<ServicePrincipalAzureCredential>;

[UsedImplicitly]
internal class UpdateServicePrincipalAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateServicePrincipalAzureCredentialCommand, ServicePrincipalAzureCredential>
{
    public async Task<ServicePrincipalAzureCredential> Handle(UpdateServicePrincipalAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = await dbContext.ServicePrincipalCredentials
            .FirstOrDefaultAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken)
            ?? throw new NotFoundException<ServicePrincipalAzureCredential>(request.AzureCredentialId);
        
        credential.AzureCredentialName = request.AzureCredentialName;
        credential.TenantId = request.TenantId;
        credential.ClientId = request.ClientId;

        if (request.ClientSecret is not null)
        {
            credential.ClientSecret = request.ClientSecret;
        }
        
        credential.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}