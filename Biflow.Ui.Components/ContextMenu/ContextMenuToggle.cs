using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Biflow.Ui.Components;

public class ContextMenuToggle(ContextMenuService contextMenuService) : ComponentBase
{
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    [Parameter] public string? CssClass { get; set; }

    /// <summary>
    /// Delegate for generating css class string to be added to the container tag.
    /// The value of the bool is true when the toggle has opened the context menu.
    /// </summary>
    [Parameter] public Func<bool, string?>? CssClassDelegate { get; set; } 

    [Parameter] public string ContainerHtmlTag { get; set; } = "div";

    [Parameter] public RenderFragment<ContextMenuToggle>? ChildContent { get; set; }

    [Parameter] public RenderFragment? MenuContent { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public bool AlsoOverrideOnClick { get; set; }

    private readonly ContextMenuService _contextMenuService = contextMenuService;

    private bool _isShowing;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ContainerHtmlTag);
        builder.AddMultipleAttributes(1, InputAttributes);
        var cssClass = $"context-menu-toggle {(_isShowing ? "open" : null)} {CssClass} {CssClassDelegate?.Invoke(_isShowing)}";
        builder.AddAttribute(2, "class", cssClass);
        builder.AddAttribute(3, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, ShowContextMenuAsync));
        builder.AddEventPreventDefaultAttribute(4, "oncontextmenu", true);
        if (AlsoOverrideOnClick)
        {
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, ShowContextMenuAsync));
        }
        if (ChildContent is not null)
        {
            builder.AddContent(6, ChildContent(this));
        }
        builder.CloseElement();
    }

    public async Task ShowContextMenuAsync(MouseEventArgs e)
    {
        if (Disabled)
        {
            return;
        }
        _isShowing = true;
        StateHasChanged();
        await _contextMenuService.ShowContextMenuAsync(e, MenuContent);
        _isShowing = false;
        StateHasChanged();
    }
}
