using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Biflow.Ui.SourceGeneration;

internal static class Extensions
{
    public static string GetPropertyNameFromIconName(this string iconName)
    {
        string[] segments = iconName.Split('-');
        var propertyName = string.Join("", segments.Select(segment => segment.Substring(0, 1).ToUpper() + segment.Substring(1)));
        if (!SyntaxFacts.IsValidIdentifier(propertyName))
        {
            // e.g. "123" => "_123"
            propertyName = "_" + propertyName;
        }
        return propertyName;
    }
}

