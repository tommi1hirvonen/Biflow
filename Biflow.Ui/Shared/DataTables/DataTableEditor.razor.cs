using Biflow.Ui.TableEditor;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.SqlServer.Query.Internal.SqlServerOpenJsonExpression;

namespace Biflow.Ui.Shared.DataTables;

public partial class DataTableEditor : ComponentBase
{
    [Inject] private ToasterService Toaster { get; set; } = null!;
    
    [Inject] private IHxMessageBoxService MessageBox { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public MasterDataTable? Table { get; set; }

    private readonly List<(string Column, bool Descending)> orderBy = [];
    private readonly HashSet<string> columnSelections = [];

    private TableData? tableData;
    private FilterSet? filterSet;
    private Dictionary<Column, HashSet<object?>>? quickFilters;
    private FilterSetOffcanvas? filterSetOffcanvas;
    private HxOffcanvas? tableInfoOffcanvas;
    private bool editModeEnabled = true;
    private bool exporting = false;
    private bool discardChanges = false; // used to prevent double confirmation when switching tables
    private bool initialLoad;

    private int TopRows
    {
        get => _topRows;
        set => _topRows = value > 0 ? value : _topRows;
    }

    private int _topRows = 100;

    private bool IsColumnSelected(string column) =>
        columnSelections is null || columnSelections.Count == 0 || columnSelections.Contains(column);

    protected override Task OnParametersSetAsync()
    {
        if (Table is not null && !initialLoad)
        {
            initialLoad = true;
            return ReloadDataAsync();
        }
        return Task.CompletedTask;
    }

    private Row[]? GetOrderedRowRecords()
    {
        if (tableData is null || quickFilters is null)
        {
            return null;
        }
        var rows = tableData.Rows
            .Where(r =>  quickFilters.All(f => f.Value.Count == 0 || f.Value.Contains(LookupValueOrValue(r, f.Key))))
            .OrderBy(r => !r.StickToTop);
        foreach (var orderBy in orderBy)
        {
            var column = tableData.Columns.First(c => c.Name == orderBy.Column);
            if (orderBy.Descending)
            {
                rows = rows.ThenByDescending(row => LookupValueOrValue(row, column));
            }
            else
            {
                rows = rows.ThenBy(row => LookupValueOrValue(row, column));
            }
        }
        return [.. rows];
    }

    private IEnumerable<object?> GetColumnValues(Column column) => tableData?.Rows
        .Select(row => LookupValueOrValue(row, column))
        .Distinct()
        .Order()
        .AsEnumerable() ?? [];

    // Helper function to retrieve lookup display value if it exists and the value itself when it does not.
    private static object? LookupValueOrValue(Row r, Column c)
    {
        var value = r.Values[c.Name];
        var lookupValue = c.Lookup?.Values.FirstOrDefault(v => v.Value?.Equals(value) == true);
        return lookupValue?.DisplayValue ?? value;
    }

    private void ToggleOrderBy(string column)
    {
        // ascending (false) => descending (true) => removed
        var index = orderBy.FindIndex(o => o.Column == column);
        if (index >= 0)
        {
            var orderBy = this.orderBy[index];
            this.orderBy.Remove(orderBy);
            if (!orderBy.Descending)
            {
                this.orderBy.Insert(index, (column, true));
            }
        }
        else
        {
            orderBy.Insert(0, (column, false));
        }
    }

    private async Task ReloadDataAsync()
    {
        if (tableData?.HasChanges == true && !discardChanges)
        {
            var confirmed = await MessageBox.ConfirmAsync("Discard unsaved changes?");
            if (!confirmed)
            {
                return;
            }
        }
        discardChanges = false;
        tableData = null;
        StateHasChanged();

        if (Table is null)
        {
            Toaster.AddError("Error loading data", $"Selected table was null.");
            return;
        }

        try
        {
            tableData = await Table.LoadDataAsync(TopRows, filterSet);
            filterSet ??= tableData.EmptyFilterSet;
            quickFilters ??= tableData.Columns.ToDictionary(c => c, _ => new HashSet<object?>());
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error loading data", ex.Message);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (tableData is null)
        {
            Toaster.AddError("Error saving changes", $"Table editor dataset object was null.");
            return;
        }

        try
        {
            var (inserted, updated, deleted) = await tableData.SaveChangesAsync();
            var message = new StringBuilder();
            if (inserted == 0 && updated == 0 && deleted == 0)
            {
                message.Append("No changes detected");
            }
            if (inserted > 0)
            {
                message.Append("Inserted ").Append(inserted).Append(" record(s)").AppendLine();
            }
            if (updated > 0)
            {
                message.Append("Updated ").Append(updated).Append(" record(s)").AppendLine();
            }
            if (deleted > 0)
            {
                message.Append("Deleted ").Append(deleted).Append(" record(s)").AppendLine();
            }
            Toaster.AddSuccess("Changes saved", message.ToString());
            tableData = await Table.LetAsync(x => x.LoadDataAsync(TopRows, filterSet)) ?? tableData;
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error saving changes", $"Error while committing changes to the database. No changes were made.{System.Environment.NewLine}{ex.Message}");
        }
    }

    private async Task DownloadExportAsync(bool filtered)
    {
        exporting = true;
        try
        {
            ArgumentNullException.ThrowIfNull(Table);
            var filterSet = filtered ? this.filterSet : null;
            var dataset = await Table.LoadDataAsync(filters: filterSet);
            using var stream = dataset.GetExcelExportStream();

            var regexSearch = new string(Path.GetInvalidFileNameChars());
            var regex = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var tableName = Table is not null ? regex.Replace(Table.DataTableName, "") : "export";
            var fileName = $"{tableName}.xlsx";
            using var streamRef = new DotNetStreamReference(stream: stream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error exporting", ex.Message);
        }
        finally
        {
            exporting = false;
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await MessageBox.ConfirmAsync("", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
        discardChanges = true;
    }
}
