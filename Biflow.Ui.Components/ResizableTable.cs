using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Biflow.Ui.Components;

public class ResizableTable(IJSRuntime js) : ComponentBase, IAsyncDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? InputAttributes { get; set; }

    [Parameter] public EventCallback<ResizableTableColumnWidth> ColumnWidthSet { get; set; }

    private readonly IJSRuntime _js = js;

    private DotNetObjectReference<ResizableTable>? _dotNetObject;
    private IJSObjectReference? _jsObject;
    private ElementReference _tableElement;

    protected override void OnInitialized()
    {
        _dotNetObject = DotNetObjectReference.Create(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "table");
        var cssClass = $"table table-resizable {CssClass}";
        builder.AddAttribute(1, "class", cssClass);
        builder.AddMultipleAttributes(2, InputAttributes);
        builder.AddElementReferenceCapture(3, element => _tableElement = element);
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
            _jsObject = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Biflow.Ui.Components/ResizableTable.js");
        }
        if (_jsObject is not null && _dotNetObject is not null)
        {
            await _jsObject.InvokeVoidAsync("createResizableTable", _tableElement, _dotNetObject);
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
        if (_jsObject is not null)
        {
            try
            {
                await _jsObject.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        _dotNetObject?.Dispose();
    }
}
