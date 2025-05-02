namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateProxy
{
    public required string ProxyName { get; init; }
    public required string ProxyUrl { get; init; }
    public string? ApiKey { get; init; }
}