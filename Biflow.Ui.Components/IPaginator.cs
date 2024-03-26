namespace Biflow.Ui.Components;

public interface IPaginator
{
    public int PageSize { get; }

    public Task SetPageSize(int size);

    public int CurrentPage { get; }

    public int PageCount { get; }

    public Task SetPage(int page);

    public Task PreviousPage();

    public Task NextPage();

    public Task LastPage();
}
