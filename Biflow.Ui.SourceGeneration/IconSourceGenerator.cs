using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.IO;
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

        var separator = Path.DirectorySeparatorChar;

        var icons = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".svg"))
            .Select((text, cancellationToken) => new IconData(text.Path, text.GetText(cancellationToken)?.ToString()))
            .Collect();

        var classIconMatches = classes.Combine(icons);

        context.RegisterSourceOutput(classes.Combine(icons), GenerateCode);
    }

    private static IconsClassData? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Attributes.FirstOrDefault() is AttributeData data)
        {
            var args = data.ConstructorArguments[0].Values.Select(v => v.Value?.ToString()).ToArray();
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
                @namespace,
                context.TargetSymbol.Name,
                args,
                incorrectModifiers,
                context.TargetSymbol.Locations.FirstOrDefault());
        }
        return null;
    }

    private static void GenerateCode(SourceProductionContext context, (ImmutableArray<IconsClassData?> Left, ImmutableArray<IconData> Right) tuple)
    {
        var (classInfos, generationInfos) = tuple;
        foreach (var classInfo in classInfos.OfType<IconsClassData>())
        {
            if (classInfo.IncorrectModifiers)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.IncorrectModifiers, classInfo.Location, classInfo.ClassName);
                context.ReportDiagnostic(diagnostic);
                continue;
            }
            var sourceBuilder = new StringBuilder($$"""
                using Biflow.Ui.Icons;

                namespace {{classInfo.Namespace}};
                
                public static partial class {{classInfo.ClassName}}
                {

                """);

            foreach (var generationInfo in generationInfos.Where(g => g.IconPath.Contains(classInfo.IconsPath)))
            {
                var propertyName = generationInfo.IconName.GetPropertyNameFromIconName();

                sourceBuilder.AppendLine($"""""""
                    public static Svg {propertyName} => new Svg(""""""
                    {generationInfo.IconText}
                    """""");

                    """"""");
            }

            sourceBuilder.Append("}");
            
            context.AddSource($"{classInfo.ClassName}.g.cs", sourceBuilder.ToString());
        }
    }
}