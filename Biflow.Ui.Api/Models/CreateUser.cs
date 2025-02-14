namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateUser
{
    public required string Username { get; init; }
    public required string? Email { get; init; }
    public required bool AuthorizeAllJobs { get; init; }
    public required bool AuthorizeAllDataTables { get; init; }
    public required Guid[] AuthorizedJobIds { get; init; }
    public required Guid[] AuthorizedDataTableIds { get; init; }
    public required UserRole MainRole { get; init; }
    public required bool IsSettingsEditor { get; init; }
    public required bool IsDataTableMaintainer { get; init; }
    public required bool IsVersionManager { get; init; }
    public string? Password { get; init; }
}