using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace Biflow.Ui.SourceGeneration;

[Generator]
internal class FeatherIconSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var separator = Path.DirectorySeparatorChar;

        var icons = context.AdditionalTextsProvider
            .Where(text => text.Path.Contains($"wwwroot{separator}icons{separator}feather") && text.Path.EndsWith(".svg"))
            .Select((text, cancellationToken) =>
            {
                return (text.Path, Text: text.GetText(cancellationToken)?.ToString());
            })
            .Where(text => text.Text is not null)
            .Collect();

        context.RegisterSourceOutput(icons, GenerateCode);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<(string Path, string Text)> values)
    {
        var sourceBuilder = new StringBuilder("""
                namespace Biflow.Ui.Components;
                
                public class FeatherIcon : IconBase
                {
                    private FeatherIcon(string svgText)
                    {
                        SvgText = svgText;
                    }


                """);

        foreach (var (path, text) in values)
        {
            var iconName = Path.GetFileNameWithoutExtension(path);
            var propertyName = iconName.GetPropertyNameFromIconName();

            sourceBuilder.AppendLine($"""""""
                    public static FeatherIcon {propertyName} => new FeatherIcon(""""""
                    {text}
                    """""");

                    """"""");
        }

        sourceBuilder.Append("}");

        context.AddSource("FeatherIcon.g.cs", sourceBuilder.ToString());
    }
}