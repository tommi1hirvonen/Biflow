using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Text;
using System.Text.RegularExpressions;

namespace Biflow.Ui.Shared.DataTables;

public partial class DataTableEditorComponent : ComponentBase
{
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private IHxMessageBoxService MessageBox { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public MasterDataTable? Table { get; set; }

    private readonly List<(string Column, bool Descending)> orderBy = [];
    private readonly HashSet<string> columnSelections = [];

    private TableData? tableData;
    private FilterSet? filterSet;
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
        var rows = tableData?.Rows.OrderBy(r => !r.IsNewRow);
        foreach (var orderBy in orderBy)
        {
            var column = tableData?.Columns.FirstOrDefault(c => c.Name == orderBy.Column);
            var lookup = column?.Lookup;

            // Helper function to retrieve lookup display value if it exists and the value itself when it does not.
            object? lookupValueOrValue(Row r)
            {
                var value = r.Values[orderBy.Column];
                var lookupValue = lookup?.Values.FirstOrDefault(v => v.Value?.Equals(value) == true);
                return lookupValue?.DisplayValue ?? value;
            };

            if (orderBy.Descending)
            {
                rows = rows?.ThenByDescending(lookupValueOrValue);
            }
            else
            {
                rows = rows?.ThenBy(lookupValueOrValue);
            }
        }
        return rows?.ToArray();
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
            Messenger.AddError("Error loading data", $"Selected table was null.");
            return;
        }

        try
        {
            tableData = await Table.LoadDataAsync(TopRows, filterSet);
            filterSet ??= tableData.EmptyFilterSet;
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error loading data", ex.Message);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (tableData is null)
        {
            Messenger.AddError("Error saving changes", $"Table editor dataset object was null.");
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
            Messenger.AddInformation("Changes saved", message.ToString());
            tableData = await Table.LetAsync(x => x.LoadDataAsync(TopRows, filterSet)) ?? tableData;
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error saving changes", $"Error while committing changes to the database. No changes were made.{System.Environment.NewLine}{ex.Message}");
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
            Messenger.AddError("Error exporting", ex.Message);
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
