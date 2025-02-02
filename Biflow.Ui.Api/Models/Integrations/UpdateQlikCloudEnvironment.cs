namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateQlikCloudEnvironment
{
    public required string QlikCloudEnvironmentName { get; init; }
    public required string EnvironmentUrl { get; init; }
    public string? ApiToken { get; init; }
}