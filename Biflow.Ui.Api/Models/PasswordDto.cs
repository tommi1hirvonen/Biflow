namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record PasswordDto
{
    public required string Password { get; init; }
}