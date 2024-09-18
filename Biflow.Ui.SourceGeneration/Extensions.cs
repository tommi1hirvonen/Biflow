using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
    {
        string @namespace = string.Empty;

        SyntaxNode? potentialNamespaceParent = syntax.Parent;

        while (potentialNamespaceParent != null &&
                potentialNamespaceParent is not NamespaceDeclarationSyntax
                && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            @namespace = namespaceParent.Name.ToString();

            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                @namespace = $"{namespaceParent.Name}.{@namespace}";
                namespaceParent = parent;
            }
        }

        return @namespace;
    }
}

