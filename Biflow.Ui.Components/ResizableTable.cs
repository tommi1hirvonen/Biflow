using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Biflow.Ui.Components;

public class ResizableTable : ComponentBase, IAsyncDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    [Parameter] public EventCallback<ResizableTableColumnWidth> ColumnWidthSet { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = null!;

    private DotNetObjectReference<ResizableTable>? dotNetObject;
    private IJSObjectReference? jsObject;
    private ElementReference tableElement;

    protected override void OnInitialized()
    {
        dotNetObject = DotNetObjectReference.Create(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "table");
        var cssClass = $"table table-resizable {CssClass}";
        builder.AddAttribute(1, "class", cssClass);
        builder.AddMultipleAttributes(2, InputAttributes);
        builder.AddElementReferenceCapture(3, element => tableElement = element);
        if (ChildContent is not null)
        {
            builder.AddContent(4, ChildContent);
        }
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            jsObject = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Biflow.Ui.Components/ResizableTable.js");
        }
        if (jsObject is not null && dotNetObject is not null)
        {
            await jsObject.InvokeVoidAsync("createResizableTable", tableElement, dotNetObject);
        }
    }

    [JSInvokable]
    public async Task SetColumnWidthAsync(string columnHeaderElementId, string width)
    {
        var columnWidth = new ResizableTableColumnWidth(columnHeaderElementId, width);
        await ColumnWidthSet.InvokeAsync(columnWidth);
    }

    public async ValueTask DisposeAsync()
    {
        if (jsObject is not null)
        {
            try
            {
                await jsObject.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        dotNetObject?.Dispose();
    }
}
