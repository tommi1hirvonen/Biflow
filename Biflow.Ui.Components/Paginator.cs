using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Biflow.Ui.Components;

public class Paginator<TItem> : ComponentBase
{
    [Parameter] public RenderFragment<IEnumerable<TItem>>? ChildContent { get; set; }

    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    public int PageSize { get; private set; } = 25;

    public int CurrentPage { get; private set; } = 1;

    public IEnumerable<TItem> PageItems => Items
        .Skip(PageSize * (CurrentPage - 1))
        .Take(PageSize);

    public int PageCount
    {
        get
        {
            var count = Items.TryGetNonEnumeratedCount(out var c)
                ? c
                : Items.Count();
            var pages = (int)Math.Ceiling(count / (double)PageSize);
            return pages == 0 ? 1 : pages;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(PageItems));
        }
    }

    protected override void OnParametersSet()
    {
        var pages = PageCount;
        if (CurrentPage > pages)
        {
            CurrentPage = pages;
        }
    }

    public void SetPageSize(int size)
    {
        PageSize = size;
        var pages = PageCount;
        if (CurrentPage > pages)
        {
            CurrentPage = pages;
        }
        StateHasChanged();
    }

    public void SetPage(int page)
    {
        if (page > 0 && page <= PageCount)
        {
            CurrentPage = page;
            StateHasChanged();
        }
    }

    public void PreviousPage()
    {
        if (CurrentPage == 1)
        {
            return;
        }
        CurrentPage--;
        StateHasChanged();
    }

    public void NextPage()
    {
        if (CurrentPage < PageCount)
        {
            CurrentPage++;
            StateHasChanged();
        }
    }

    public void LastPage()
    {
        CurrentPage = PageCount;
        StateHasChanged();
    }
}
