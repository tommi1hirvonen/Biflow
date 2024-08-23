using System.IO;

namespace Biflow.Ui.SourceGeneration;

internal class IconData(string iconPath, string? iconText)
{
    public string IconPath { get; } = iconPath;

    public string IconName { get; } = Path.GetFileNameWithoutExtension(iconPath);

    public string? IconText { get; } = iconText;
}
