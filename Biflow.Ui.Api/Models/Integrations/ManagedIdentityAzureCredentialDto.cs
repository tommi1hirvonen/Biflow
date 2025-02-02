namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record ManagedIdentityAzureCredentialDto
{
    public required string AzureCredentialName { get; init; }
    public string? ClientId { get; init; }
}