using Biflow.Ui.TableEditor;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text;
using System.Text.RegularExpressions;

namespace Biflow.Ui.Shared.DataTables;

public partial class DataTableEditor(ToasterService toaster, IHxMessageBoxService messageBox, IJSRuntime js) : ComponentBase
{
    [Parameter] public MasterDataTable? Table { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _messageBox = messageBox;
    private readonly IJSRuntime _js = js;
    private readonly List<(string Column, bool Descending)> _orderBy = [];
    private readonly HashSet<string> _columnSelections = [];
    private readonly Dictionary<string, HashSet<object?>> _quickFilters = [];
    private readonly Dictionary<string, string> _columnWidths = [];
    private readonly Dictionary<string, Column> _columns = [];

    private TableData? _tableData;
    private FilterSet? _filterSet;
    private FilterSetOffcanvas? _filterSetOffcanvas;
    private HxOffcanvas? _tableInfoOffcanvas;
    private bool _editModeEnabled = true;
    private bool _exporting = false;
    private bool _discardChanges = false; // used to prevent double confirmation when switching tables
    private bool _initialLoad;

    private int TopRows
    {
        get;
        set => field = value > 0 ? value : field;
    } = 100;

    private bool IsColumnSelected(string column) =>
        _columnSelections.Count == 0 || _columnSelections.Contains(column);

    protected override Task OnParametersSetAsync()
    {
        if (Table is null || _initialLoad)
        {
            return Task.CompletedTask;
        }
        _initialLoad = true;
        return ReloadDataAsync();
    }

    private Row[]? GetOrderedRowRecords()
    {
        if (_tableData is null)
        {
            return null;
        }
        var rows = _tableData.Rows
            .Where(r => 
                _quickFilters
                    .All(f => f.Value.Count == 0 ||
                              _columns.TryGetValue(f.Key, out var column) && 
                              f.Value.Contains(LookupValueOrValue(r, column))))
            .OrderBy(r => !r.StickToTop);
        foreach (var orderBy in _orderBy)
        {
            var column = _tableData.Columns.First(c => c.Name == orderBy.Column);
            rows = orderBy.Descending
                ? rows.ThenByDescending(row => LookupValueOrValue(row, column))
                : rows.ThenBy(row => LookupValueOrValue(row, column));
        }
        return [.. rows];
    }

    private IEnumerable<object?> GetColumnValues(Column column) => _tableData?.Rows
        .Select(row => LookupValueOrValue(row, column))
        .Distinct()
        .OrderBy(row => row?.ToString())
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
        var index = _orderBy.FindIndex(o => o.Column == column);
        if (index >= 0)
        {
            var orderBy = _orderBy[index];
            _orderBy.Remove(orderBy);
            if (!orderBy.Descending)
            {
                _orderBy.Insert(index, (column, true));
            }
        }
        else
        {
            _orderBy.Insert(0, (column, false));
        }
    }

    private async Task ReloadDataAsync()
    {
        if (_tableData?.HasChanges == true && !_discardChanges)
        {
            var confirmed = await _messageBox.ConfirmAsync("Discard unsaved changes?");
            if (!confirmed)
            {
                return;
            }
        }
        _discardChanges = false;
        _tableData = null;
        StateHasChanged();

        if (Table is null)
        {
            _toaster.AddError("Error loading data", $"Selected table was null.");
            return;
        }

        try
        {
            _tableData = await Table.LoadDataAsync(TopRows, _filterSet);
            _filterSet ??= _tableData.EmptyFilterSet;
            _columns.Clear();
            foreach (var column in _tableData.Columns)
            {
                _columns[column.Name] = column;
            }
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error loading data", ex.Message);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (_tableData is null)
        {
            _toaster.AddError("Error saving changes", $"Table editor dataset object was null.");
            return;
        }

        try
        {
            var (inserted, updated, deleted) = await _tableData.SaveChangesAsync();
            var message = new StringBuilder();
            switch (inserted)
            {
                case 0 when updated == 0 && deleted == 0:
                    message.Append("No changes detected");
                    break;
                case > 0:
                    message.Append("Inserted ").Append(inserted).Append(" record(s)").AppendLine();
                    break;
            }
            if (updated > 0)
            {
                message.Append("Updated ").Append(updated).Append(" record(s)").AppendLine();
            }
            if (deleted > 0)
            {
                message.Append("Deleted ").Append(deleted).Append(" record(s)").AppendLine();
            }
            _toaster.AddSuccess("Changes saved", message.ToString());
            _tableData = await Table.LetAsync(x => x.LoadDataAsync(TopRows, _filterSet)) ?? _tableData;
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error saving changes", $"Error while committing changes to the database. No changes were made.{Environment.NewLine}{ex.Message}");
        }
    }

    private async Task DownloadExportAsync(bool filtered)
    {
        _exporting = true;
        try
        {
            ArgumentNullException.ThrowIfNull(Table);
            var filterSet = filtered ? _filterSet : null;
            var dataset = await Table.LoadDataAsync(filters: filterSet);
            await using var stream = dataset.GetExcelExportStream();

            var regexSearch = new string(Path.GetInvalidFileNameChars());
            var regex = new Regex($"[{Regex.Escape(regexSearch)}]");
            var tableName = Table is not null ? regex.Replace(Table.DataTableName, "") : "export";
            var fileName = $"{tableName}.xlsx";
            using var streamRef = new DotNetStreamReference(stream: stream);
            await _js.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error exporting", ex.Message);
        }
        finally
        {
            _exporting = false;
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await _messageBox.ConfirmAsync("", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
        _discardChanges = true;
    }
}
