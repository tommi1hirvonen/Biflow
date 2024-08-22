using System.IO;

namespace Biflow.Ui.SourceGeneration;

class IconsClassData(string @namespace, string className, string?[] pathSegments)
{
    public string Namespace { get; } = @namespace;

    public string ClassName { get; } = className;

    public string IconsPath { get; } = string.Join(Path.DirectorySeparatorChar.ToString(), pathSegments);
}
