namespace Biflow.Ui.Components;

public record DependencyGraphNode(string Id, string Name, string CssClass, string TooltipText, bool EnableOnClick, bool Rounded = true);