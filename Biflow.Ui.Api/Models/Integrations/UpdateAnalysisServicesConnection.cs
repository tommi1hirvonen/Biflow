namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateAnalysisServicesConnection
{
    public required string ConnectionName { get; init; }
    public string? ConnectionString { get; init; }
    public Guid? CredentialId { get; init; }
}