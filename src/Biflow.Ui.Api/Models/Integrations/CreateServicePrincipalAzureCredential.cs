namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CreateServicePrincipalAzureCredential
{
    public required string AzureCredentialName { get; init; }
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}