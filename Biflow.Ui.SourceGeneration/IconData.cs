using System.IO;

namespace Biflow.Ui.SourceGeneration;

class IconData(string iconPath, string? iconText)
{
    public string IconPath { get; } = iconPath;

    public string IconName { get; } = Path.GetFileNameWithoutExtension(iconPath);

    public string? IconText { get; } = iconText;
}
