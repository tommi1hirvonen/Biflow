using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Biflow.Ui.Components;

public class Paginator<TItem> : ComponentBase, IPaginator
{
    [Parameter] public RenderFragment<IEnumerable<TItem>?>? ChildContent { get; set; }

    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    [Parameter] public EventCallback<int> OnPageSizeChanged { get; set; }

    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    [Parameter] public int InitialPageSize { get; set; }

    [Parameter] public int InitialPage { get; set; }

    public int PageSize { get; private set; } = 25;

    public int CurrentPage { get; private set; } = 1;

    public int CurrentItemCount => PageItems?.Count() ?? 0;

    public int TotalItemCount
    {
        get
        {
            var items = Items ?? [];
            var count = items.TryGetNonEnumeratedCount(out var c)
                ? c
                : items.Count();
            return count;
        }
    }

    private IEnumerable<TItem>? PageItems => Items?
        .Skip(PageSize * (CurrentPage - 1))
        .Take(PageSize);

    public int PageCount
    {
        get
        {
            var items = Items ?? [];
            var count = items.TryGetNonEnumeratedCount(out var c)
                ? c
                : items.Count();
            var pages = (int)Math.Ceiling(count / (double)PageSize);
            return pages == 0 ? 1 : pages;
        }
    }

    private bool initialPageSizeSet = false;
    private bool initialPageSet = false;
    private bool itemsSet = false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(PageItems));
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Items is not null && Items.Any())
        {
            itemsSet = true;
        }

        if (InitialPageSize > 0 && !initialPageSizeSet)
        {
            initialPageSizeSet = true;
            PageSize = InitialPageSize;
        }

        // Set initial page only after items have been set.
        // Otherwise, the current page might be reset in the next step.
        if (InitialPage > 0 && !initialPageSet && itemsSet)
        {
            initialPageSet = true;
            CurrentPage = InitialPage;
        }

        var pages = PageCount;
        if (CurrentPage > pages)
        {
            CurrentPage = pages;
            await OnPageChanged.InvokeAsync(CurrentPage);
        }
    }

    public async Task SetPageSize(int size)
    {
        PageSize = size;
        var pages = PageCount;
        if (CurrentPage > pages)
        {
            CurrentPage = pages;
        }
        await OnPageSizeChanged.InvokeAsync(PageSize);
        await OnPageChanged.InvokeAsync(CurrentPage);
        StateHasChanged();
    }

    public async Task SetPage(int page)
    {
        if (page > 0 && page <= PageCount)
        {
            CurrentPage = page;
            await OnPageChanged.InvokeAsync(CurrentPage);
            StateHasChanged();
        }
    }

    public async Task PreviousPage()
    {
        if (CurrentPage == 1)
        {
            return;
        }
        CurrentPage--;
        await OnPageChanged.InvokeAsync(CurrentPage);
        StateHasChanged();
    }

    public async Task NextPage()
    {
        if (CurrentPage < PageCount)
        {
            CurrentPage++;
            await OnPageChanged.InvokeAsync(CurrentPage);
            StateHasChanged();
        }
    }

    public async Task LastPage()
    {
        CurrentPage = PageCount;
        await OnPageChanged.InvokeAsync(CurrentPage);
        StateHasChanged();
    }
}
