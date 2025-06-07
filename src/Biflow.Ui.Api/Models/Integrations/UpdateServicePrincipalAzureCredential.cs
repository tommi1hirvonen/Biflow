namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateServicePrincipalAzureCredential
{
    public required string AzureCredentialName { get; init; }
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public string? ClientSecret { get; init; }
}