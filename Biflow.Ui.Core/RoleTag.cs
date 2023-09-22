using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public readonly struct RoleTag : ITag
{
    public RoleTag(string role)
    {
        TagName = role;
        Color = role switch
        {
            "Admin" => TagColor.Red,
            "Editor" => TagColor.Yellow,
            "Operator" => TagColor.Purple,
            _ => TagColor.DarkGray
        };
    }

    public Guid TagId => Guid.Empty;

    public string TagName { get; }

    public TagColor Color { get; }
}
