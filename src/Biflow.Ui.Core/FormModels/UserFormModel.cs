using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Core;

public class UserFormModel(User user, PasswordModel? passwordModel)
{
    public Guid UserId { get; } = user.UserId;
    
    [Required]
    [MaxLength(250)]
    public string Username { get; set; } = user.Username;

    [MaxLength(254)]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; } = user.Email;

    public bool AuthorizeAllJobs { get; set; } = user.AuthorizeAllJobs;

    public bool AuthorizeAllDataTables { get; set; } = user.AuthorizeAllDataTables;

    public PasswordModel? PasswordModel { get; } = passwordModel;

    public UserRole Role { get; set; } = user.Roles switch
    {
        var x when x.Contains(Roles.Admin) => UserRole.Admin,
        var x when x.Contains(Roles.Editor) => UserRole.Editor,
        var x when x.Contains(Roles.Operator) => UserRole.Operator,
        var x when x.Contains(Roles.Viewer) => UserRole.Viewer,
        _ => UserRole.Viewer
    };

    public bool IsSettingsEditor { get; set; } = user.Roles.Contains(Roles.SettingsEditor);
    
    public bool IsDataTableMaintainer { get; set; } = user.Roles.Contains(Roles.DataTableMaintainer);
    
    public bool IsVersionManager { get; set; } = user.Roles.Contains(Roles.VersionManager);

    public HashSet<Guid> AuthorizedJobIds { get; } =
        user.Jobs.Select(j => j.JobId).Distinct().ToHashSet();

    public HashSet<Guid> AuthorizedDataTableIds { get; } =
        user.DataTables.Select(t => t.DataTableId).Distinct().ToHashSet();
}
