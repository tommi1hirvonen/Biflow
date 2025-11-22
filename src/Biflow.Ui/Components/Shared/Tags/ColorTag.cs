namespace Biflow.Ui.Components.Shared.Tags;

public readonly struct ColorTag(string colorName, TagColor color) : ITag
{
    public Guid TagId => Guid.Empty;

    public string TagName => colorName;

    public TagColor Color => color;

    public int SortOrder => 0;
}