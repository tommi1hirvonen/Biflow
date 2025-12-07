namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record ProxyDto
{
    public required string ProxyName { get; init; }
    public required string ProxyUrl { get; init; }
    public string? ApiKey { get; init; }
    public int MaxConcurrentExeSteps { get; init; }
}