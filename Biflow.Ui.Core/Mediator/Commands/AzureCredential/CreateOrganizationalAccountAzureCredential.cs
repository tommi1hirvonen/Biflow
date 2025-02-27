namespace Biflow.Ui.Core;

public record CreateOrganizationalAccountAzureCredentialCommand(
    string AzureCredentialName,
    string TenantId,
    string ClientId,
    string Username,
    string Password) : IRequest<OrganizationalAccountAzureCredential>;

[UsedImplicitly]
internal class CreateOrganizationalAccountAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateOrganizationalAccountAzureCredentialCommand, OrganizationalAccountAzureCredential>
{
    public async Task<OrganizationalAccountAzureCredential> Handle(CreateOrganizationalAccountAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = new OrganizationalAccountAzureCredential
        {
            AzureCredentialName = request.AzureCredentialName,
            TenantId = request.TenantId,
            ClientId = request.ClientId,
            Username = request.Username,
            Password = request.Password
        };
        
        credential.EnsureDataAnnotationsValidated();
        
        dbContext.OrganizationalAccountCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}