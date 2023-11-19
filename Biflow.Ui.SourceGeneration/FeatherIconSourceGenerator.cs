using Microsoft.CodeAnalysis;
using System.IO;
using System.Text;

namespace Biflow.Ui.SourceGeneration;

[Generator]
internal class FeatherIconSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            if (Path.GetFileNameWithoutExtension(tree.FilePath) != "CxIcon")
            {
                continue;
            }

            var sourceBuilder = new StringBuilder("""
                namespace Biflow.Ui.Components;
                
                public class FeatherIcon : IconBase
                {
                    private FeatherIcon(string svgText)
                    {
                        SvgText = svgText;
                    }


                """);

            var iconsDirectory = Path.Combine(Path.GetDirectoryName(tree.FilePath), "..\\", "wwwroot", "icons", "feather");
            var svgFiles = Directory.GetFiles(iconsDirectory, "*.svg");

            foreach (var file in svgFiles)
            {
                var svgText = File.ReadAllText(file);
                var iconName = Path.GetFileNameWithoutExtension(file);
                var propertyName = iconName.GetPropertyNameFromIconName();

                sourceBuilder.AppendLine($"""""""
                    public static FeatherIcon {propertyName} => new FeatherIcon(""""""
                    {svgText}
                    """""");

                    """"""");
            }

            sourceBuilder.Append("}");

            context.AddSource("FeatherIcon.g.cs", sourceBuilder.ToString());
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {

    }
}