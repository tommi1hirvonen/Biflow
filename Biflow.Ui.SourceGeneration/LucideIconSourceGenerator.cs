using Microsoft.CodeAnalysis;
using System.IO;
using System.Text;

namespace Biflow.Ui.SourceGeneration;

[Generator]
internal class LucideIconSourceGenerator : ISourceGenerator
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

                public class LucideIcon : IconBase
                {
                    private LucideIcon(string svgText)
                    {
                        SvgText = svgText;
                    }


                """);

            var iconsDirectory = Path.Combine(Path.GetDirectoryName(tree.FilePath), "..\\", "wwwroot", "icons", "lucide");
            var svgFiles = Directory.GetFiles(iconsDirectory, "*.svg");

            foreach (var file in svgFiles)
            {
                var svgText = File.ReadAllText(file);
                svgText = svgText.Replace(@"<svg", @"<svg class=""lucide""");
                var iconName = Path.GetFileNameWithoutExtension(file);
                var propertyName = iconName.GetPropertyNameFromIconName();

                sourceBuilder.AppendLine($"""""""
                    public static LucideIcon {propertyName} => new LucideIcon(""""""
                    {svgText}
                    """""");

                    """"""");
            }

            sourceBuilder.Append("}");

            context.AddSource("LucideIcon.g.cs", sourceBuilder.ToString());

        }

    }

    public void Initialize(GeneratorInitializationContext context)
    {

    }
}