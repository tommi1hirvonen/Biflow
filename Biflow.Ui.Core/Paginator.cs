namespace Biflow.Ui.Core;

public class Paginator<TItem>
{
    private IEnumerable<TItem> _items = [];
    private int _pageSize = 25;

    public IEnumerable<TItem> Items
    {
        get => _items;
        set
        {
            _items = value;
            var pages = PageCount;
            if (CurrentPage > pages)
            {
                CurrentPage = pages;
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            _pageSize = value;
            var pages = PageCount;
            if (CurrentPage > pages)
            {
                CurrentPage = pages;
            }
        }
    }

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

    public void SetPage(int page)
    {
        if (page > 0 && page <= PageCount)
        {
            CurrentPage = page;
        }
    }

    public void PreviousPage()
    {
        if (CurrentPage == 1)
        {
            return;
        }
        CurrentPage--;
    }

    public void NextPage()
    {
        if (CurrentPage < PageCount)
        {
            CurrentPage++;
        }
    }

    public void LastPage()
    {
        CurrentPage = PageCount;
    }
}
