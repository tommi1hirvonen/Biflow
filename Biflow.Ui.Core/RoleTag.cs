using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public readonly struct RoleTag : ITag
{
    public RoleTag(string role)
    {
        TagName = role;
        Color = role switch
        {
            Roles.Admin => TagColor.Red,
            Roles.Editor => TagColor.Yellow,
            Roles.Operator => TagColor.Purple,
            Roles.DataTableMaintainer => TagColor.Blue,
            _ => TagColor.DarkGray
        };
    }

    public Guid TagId => Guid.Empty;

    public string TagName { get; }

    public TagColor Color { get; }
}
