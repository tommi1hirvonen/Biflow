using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Biflow.Ui.Components;

public class ContextMenuToggle : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public string ContainerHtmlTag { get; set; } = "div";

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public RenderFragment? MenuContent { get; set; }

    [Parameter] public string? ContainerCssClass { get; set; }

    [Parameter] public string? DropdownCssClass { get; set; }

    private ElementReference? container;
    private ElementReference? dropdown;
    private IJSObjectReference? jsObject;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ContainerHtmlTag);
        builder.AddAttribute(1, "class", ContainerCssClass);
        builder.AddElementReferenceCapture(2, value => container = value);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", $"dropdown position-absolute {DropdownCssClass}");
        builder.AddElementReferenceCapture(6, value => dropdown = value);
        builder.OpenElement(7, "ul");
        builder.AddAttribute(8, "class", "dropdown-menu context-menu");
        builder.AddContent(9, MenuContent);
        builder.CloseElement();
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ArgumentNullException.ThrowIfNull(container);
            jsObject = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Biflow.Ui.Components/ContextMenuToggle.js");
            await jsObject.InvokeVoidAsync("setOnContextMenuListener", container, dropdown);
            await jsObject.InvokeVoidAsync("attachWindowOnClickListener");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (jsObject is not null)
        {
            try
            {
                await jsObject.InvokeVoidAsync("disposeWindowOnClickListener");
            }
            catch (JSDisconnectedException) { }
            await jsObject.DisposeAsync();
        }
    }
}
