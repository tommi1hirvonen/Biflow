using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using R = Biflow.Core.Constants.Roles;

namespace Biflow.Core.Entities;

public class User : IAuditable
{
    private readonly List<string> _roles = [R.Viewer];

    public Guid UserId { get; private set; }

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
        _roles.RemoveAll(r => r == R.Admin || r == R.Editor || r == R.Viewer || r == R.Operator);
        _roles.Add(R.Editor);
    }

    public void SetIsOperator()
    {
        _roles.RemoveAll(r => r == R.Admin || r == R.Editor || r == R.Viewer || r == R.Operator);
        _roles.Add(R.Operator);
    }

    public void SetIsViewer()
    {
        _roles.RemoveAll(r => r == R.Admin || r == R.Editor || r == R.Viewer || r == R.Operator);
        _roles.Add(R.Viewer);
    }

    public void SetIsSettingsEditor(bool enabled = true)
    {
        if (enabled && !_roles.Contains(R.Admin) && !_roles.Contains(R.SettingsEditor))
        {
            _roles.Add(R.SettingsEditor);
        }
        else if (!enabled)
        {
            _roles.Remove(R.SettingsEditor);
        }
    }

    public void SetIsDataTableMaintainer(bool enabled = true)
    {
        if (enabled && !_roles.Contains(R.Admin) && !_roles.Contains(R.DataTableMaintainer))
        {
            _roles.Add(R.DataTableMaintainer);
        }
        else if (!enabled)
        {
            _roles.Remove(R.DataTableMaintainer);
        }
    }
}
