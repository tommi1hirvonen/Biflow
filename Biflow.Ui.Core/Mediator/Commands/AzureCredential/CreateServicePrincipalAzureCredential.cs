namespace Biflow.Ui.Core;

public record CreateServicePrincipalAzureCredentialCommand(
    string AzureCredentialName,
    string TenantId,
    string ClientId,
    string ClientSecret) : IRequest<ServicePrincipalAzureCredential>;

[UsedImplicitly]
internal class CreateServicePrincipalAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateServicePrincipalAzureCredentialCommand, ServicePrincipalAzureCredential>
{
    public async Task<ServicePrincipalAzureCredential> Handle(CreateServicePrincipalAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = new ServicePrincipalAzureCredential
        {
            AzureCredentialName = request.AzureCredentialName,
            TenantId = request.TenantId,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
        };
        
        credential.EnsureDataAnnotationsValidated();
        
        dbContext.ServicePrincipalCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}