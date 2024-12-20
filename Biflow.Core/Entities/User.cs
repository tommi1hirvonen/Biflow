using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using R = Biflow.Core.Constants.Roles;

namespace Biflow.Core.Entities;

public class User : IAuditable
{
    private readonly List<string> _roles = [R.Viewer];

    public Guid UserId { get; [UsedImplicitly] private set; }

    [Required]
    [MaxLength(250)]
    public required string Username { get; set; }

    [MaxLength(254)]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    public IEnumerable<string> Roles => _roles;

    public bool AuthorizeAllJobs { get; set; }

    public bool AuthorizeAllDataTables { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    public DateTimeOffset? LastLoginOn { get; set; }

    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();

    public ICollection<Job> Jobs { get; } = new List<Job>();

    public ICollection<MasterDataTable> DataTables { get; } = new List<MasterDataTable>();

    public void SetIsAdmin()
    {
        _roles.Clear();
        _roles.Add(R.Admin);
    }

    public void SetIsEditor()
    {
        _roles.RemoveAll(r => r is R.Admin or R.Editor or R.Viewer or R.Operator);
        _roles.Add(R.Editor);
    }

    public void SetIsOperator()
    {
        _roles.RemoveAll(r => r is R.Admin or R.Editor or R.Viewer or R.Operator);
        _roles.Add(R.Operator);
    }

    public void SetIsViewer()
    {
        _roles.RemoveAll(r => r is R.Admin or R.Editor or R.Viewer or R.Operator);
        _roles.Add(R.Viewer);
    }

    public void SetIsSettingsEditor(bool enabled = true)
    {
        switch (enabled)
        {
            case true when !_roles.Contains(R.Admin) && !_roles.Contains(R.SettingsEditor):
                _roles.Add(R.SettingsEditor);
                break;
            case false:
                _roles.Remove(R.SettingsEditor);
                break;
        }
    }

    public void SetIsDataTableMaintainer(bool enabled = true)
    {
        switch (enabled)
        {
            case true when !_roles.Contains(R.Admin) && !_roles.Contains(R.DataTableMaintainer):
                _roles.Add(R.DataTableMaintainer);
                break;
            case false:
                _roles.Remove(R.DataTableMaintainer);
                break;
        }
    }

    public void SetIsVersionManager(bool enabled = true)
    {
        switch (enabled)
        {
            case true when !_roles.Contains(R.Admin) && !_roles.Contains(R.VersionManager):
                _roles.Add(R.VersionManager);
                break;
            case false:
                _roles.Remove(R.VersionManager);
                break;
        }
    }
}
