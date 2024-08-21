using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Biflow.Ui.Components;

public class SvgIcon : ComponentBase
{
    [Parameter, EditorRequired] public IconBase? Icon { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddMarkupContent(0, Icon?.SvgText);
    }
}
