namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CreateProxy
{
    public required string ProxyName { get; init; }
    public required string ProxyUrl { get; init; }
    public required string ApiKey { get; init; }
}