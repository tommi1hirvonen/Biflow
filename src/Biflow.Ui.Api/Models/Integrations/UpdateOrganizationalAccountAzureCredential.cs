namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateOrganizationalAccountAzureCredential
{
    public required string AzureCredentialName { get; init; }
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string Username { get; init; }
    public string? Password { get; init; }
}