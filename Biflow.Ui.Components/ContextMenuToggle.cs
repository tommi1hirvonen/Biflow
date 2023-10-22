using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Biflow.Ui.Components;

public class ContextMenuToggle : ComponentBase
{
    [Inject] private ContextMenuService ContextMenuService { get; set; } = null!;

    [Parameter] public string ContainerHtmlTag { get; set; } = "div";

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public RenderFragment? MenuContent { get; set; }

    [Parameter] public string? ContainerCssClass { get; set; }

    [Parameter] public bool Disabled { get; set; } = false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ContainerHtmlTag);
        builder.AddAttribute(1, "class", ContainerCssClass);
        builder.AddAttribute(2, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, ShowContextMenuAsync));
        builder.AddEventPreventDefaultAttribute(3, "oncontextmenu", true);
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }

    private async Task ShowContextMenuAsync(MouseEventArgs e)
    {
        if (Disabled)
        {
            return;
        }
        await ContextMenuService.ShowContextMenuAsync(e, MenuContent);
    }
}
