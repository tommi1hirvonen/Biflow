namespace Biflow.Ui.Core;

public readonly struct RoleTag(string role) : ITag
{
    public Guid TagId => Guid.Empty;

    public string TagName { get; } = role;

    public TagColor Color { get; } = role switch
    {
        Roles.Admin => TagColor.Red,
        Roles.Editor => TagColor.Purple,
        Roles.Operator => TagColor.Green,
        Roles.DataTableMaintainer => TagColor.Blue,
        Roles.VersionManager => TagColor.Yellow,
        Roles.SettingsEditor => TagColor.Yellow,
        _ => TagColor.DarkGray
    };

    public int SortOrder { get; } = 0;
}
