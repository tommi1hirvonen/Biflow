namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateDbtAccount
{
    public required string DbtAccountName { get; init; }
    public required string ApiBaseUrl { get; init; }
    public required string AccountId { get; init; }
    public string? ApiToken { get; init; }
}