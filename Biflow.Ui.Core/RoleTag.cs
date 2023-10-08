using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public readonly struct RoleTag(string role) : ITag
{
    public Guid TagId => Guid.Empty;

    public string TagName { get; } = role;

    public TagColor Color { get; } = role switch
    {
        Roles.Admin => TagColor.Red,
        Roles.Editor => TagColor.Yellow,
        Roles.Operator => TagColor.Purple,
        Roles.DataTableMaintainer => TagColor.Blue,
        _ => TagColor.DarkGray
    };
}
