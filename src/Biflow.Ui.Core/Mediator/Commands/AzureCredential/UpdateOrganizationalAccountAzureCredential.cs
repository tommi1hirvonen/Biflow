namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="AzureCredentialId"></param>
/// <param name="TenantId"></param>
/// <param name="ClientId"></param>
/// <param name="Username"></param>
/// <param name="Password">Pass null to retain the previous Password value</param>
public record UpdateOrganizationalAccountAzureCredentialCommand(
    Guid AzureCredentialId,
    string AzureCredentialName,
    string TenantId,
    string ClientId,
    string Username,
    string? Password) : IRequest<OrganizationalAccountAzureCredential>;

[UsedImplicitly]
internal class UpdateOrganizationalAccountAzureCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateOrganizationalAccountAzureCredentialCommand, OrganizationalAccountAzureCredential>
{
    public async Task<OrganizationalAccountAzureCredential> Handle(UpdateOrganizationalAccountAzureCredentialCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var credential = await dbContext.OrganizationalAccountCredentials
            .FirstOrDefaultAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken)
            ?? throw new NotFoundException<OrganizationalAccountAzureCredential>(request.AzureCredentialId);
        
        credential.AzureCredentialName = request.AzureCredentialName;
        credential.TenantId = request.TenantId;
        credential.ClientId = request.ClientId;
        credential.Username = request.Username;

        if (request.Password is not null)
        {
            credential.Password = request.Password;
        }
        
        credential.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return credential;
    }
}