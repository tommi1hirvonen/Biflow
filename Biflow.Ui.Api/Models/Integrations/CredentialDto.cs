namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CredentialDto
{
    public required string? Domain { get; init; }
    public required string Username { get; init; }
    public required string? Password { get; init; }
}