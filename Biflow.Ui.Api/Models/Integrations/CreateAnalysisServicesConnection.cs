namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CreateAnalysisServicesConnection
{
    public required string ConnectionName { get; init; }
    public required string ConnectionString { get; init; }
    public Guid? CredentialId { get; init; }
}