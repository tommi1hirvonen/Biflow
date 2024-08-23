using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Biflow.Ui.SourceGeneration;

[Generator]
internal class IconSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "Biflow.Ui.Icons.GenerateIconsAttribute",
                predicate: (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax,
                transform: Transform)
            .Collect();

        var icons = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".svg"))
            .Select((text, cancellationToken) => new IconData(text.Path, text.GetText(cancellationToken)?.ToString()))
            .Collect();

        var classIconMatches = classes.Combine(icons);

        context.RegisterSourceOutput(classes.Combine(icons), GenerateCode);
    }

    private static IconsClassData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == "GenerateIconsAttribute") is AttributeData data)
        {
            var args = data.ConstructorArguments[0].Values
                .Select(v => v.Value?.ToString())
                .ToArray();
            var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
            var @namespace = classDeclaration.GetNamespace();
            var incorrectModifiers = true;
            if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))
                && classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
                && classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                incorrectModifiers = false;
            }
            return new IconsClassData(
                @namespace: @namespace,
                className: context.TargetSymbol.Name,
                pathSegments: args,
                incorrectModifiers: incorrectModifiers,
                location: context.TargetSymbol.Locations.FirstOrDefault());
        }
        return null;
    }

    private static void GenerateCode(SourceProductionContext context, (ImmutableArray<IconsClassData?> Left, ImmutableArray<IconData> Right) tuple)
    {
        var (classes, icons) = tuple;
        foreach (var classData in classes.OfType<IconsClassData>())
        {
            if (classData.IncorrectModifiers)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IncorrectModifiers, classData.Location, classData.ClassName);
                context.ReportDiagnostic(diagnostic);
                continue;
            }
            var sourceBuilder = new StringBuilder($$"""
                using Biflow.Ui.Icons;

                namespace {{classData.Namespace}};
                
                public static partial class {{classData.ClassName}}
                {

                """);

            foreach (var iconData in icons.Where(g => g.IconPath.Contains(classData.IconsPath)))
            {
                var propertyName = iconData.IconName.GetPropertyNameFromIconName();

                sourceBuilder.AppendLine($"""""""
                    public static Svg {propertyName} => new Svg(""""""
                    {iconData.IconText}
                    """""");

                    """"""");
            }

            sourceBuilder.Append("}");
            
            context.AddSource($"{classData.ClassName}.g.cs", sourceBuilder.ToString());
        }
    }
}