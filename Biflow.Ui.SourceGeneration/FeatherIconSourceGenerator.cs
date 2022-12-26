using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Linq;
using System.Text;

namespace Biflow.Ui.SourceGeneration
{
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

                var sourceBuilder = new StringBuilder(@"
using System;
namespace Biflow.Ui.Components
{
    public class FeatherIcon : IconBase
    {
        private FeatherIcon(string svgText)
        {
            SvgText = svgText;
        }
");

                var iconsDirectory = Path.Combine(Path.GetDirectoryName(tree.FilePath), "wwwroot", "icons", "feather");
                var svgFiles = Directory.GetFiles(iconsDirectory, "*.svg");

                foreach (var file in svgFiles)
                {
                    var svgText = File.ReadAllText(file);
                    var iconName = Path.GetFileNameWithoutExtension(file);
                    var propertyName = GetPropertyNameFromIconName(iconName);

                    sourceBuilder.AppendLine($@"
public static FeatherIcon {propertyName} => new FeatherIcon(""""""
{svgText}
"""""");");
                }

                sourceBuilder.Append(@"
    }
}");

                context.AddSource("FeatherIcon.g.cs", sourceBuilder.ToString());

            }

        }

        private string GetPropertyNameFromIconName(string iconName)
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

        public void Initialize(GeneratorInitializationContext context)
        {

        }
    }
}