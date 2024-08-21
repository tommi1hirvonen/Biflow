using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Biflow.Ui.SourceGeneration;

[Generator]
internal class LucideIconSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var separator = Path.DirectorySeparatorChar;

        var icons = context.AdditionalTextsProvider
            .Where(text => text.Path.Contains($"wwwroot{separator}icons{separator}lucide") && text.Path.EndsWith(".svg"))
            .Select((text, cancellationToken) =>
            {
                return (text.Path, Text: text.GetText(cancellationToken)?.ToString());
            })
            .Where(text => text.Text is not null)
            .Collect();

        context.RegisterSourceOutput(icons, GenerateCode);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<(string Path, string? Text)> values)
    {
        var sourceBuilder = new StringBuilder("""
                namespace Biflow.Ui.Components;
                
                public class LucideIcon : IconBase
                {
                    private LucideIcon(string svgText)
                    {
                        SvgText = svgText;
                    }


                """);

        foreach (var (path, text) in values)
        {
            var iconName = Path.GetFileNameWithoutExtension(path);
            var propertyName = iconName.GetPropertyNameFromIconName();

            sourceBuilder.AppendLine($"""""""
                    public static LucideIcon {propertyName} => new LucideIcon(""""""
                    {text}
                    """""");

                    """"""");
        }

        sourceBuilder.Append("}");

        context.AddSource("LucideIcon.g.cs", sourceBuilder.ToString());
    }
}