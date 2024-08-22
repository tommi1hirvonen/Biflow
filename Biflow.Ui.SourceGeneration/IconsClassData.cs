using Microsoft.CodeAnalysis;
using System.IO;

namespace Biflow.Ui.SourceGeneration;

class IconsClassData(string @namespace, string className, string?[] pathSegments, bool incorrectModifiers, Location? location)
{
    public string Namespace { get; } = @namespace;

    public string ClassName { get; } = className;

    public string IconsPath { get; } = string.Join(Path.DirectorySeparatorChar.ToString(), pathSegments);

    public bool IncorrectModifiers { get; } = incorrectModifiers;

    public Location? Location { get; } = location;
}
