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

public partial class DataTableEditComponent : ComponentBase
{
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private IHxMessageBoxService MessageBox { get; set; } = null!;

    [Inject] private MarkupHelperService MarkupHelper { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public List<MasterDataTable>? Tables { get; set; }

    [Parameter] public Guid? TableId { get; set; }

    private MasterDataTable? SelectedTable { get; set; }

    private TableData? TableData { get; set; }

    private int TopRows
    {
        get => _topRows;
        set => _topRows = value > 0 ? value : _topRows;
    }

    private int _topRows = 100;

    private List<(string Column, bool Descending)> OrderBy { get; } = new();

    private FilterSet? FilterSet { get; set; }

    private Dictionary<string, bool>? ColumnSelections { get; set; }

    private bool IsColumnSelected(string column) => (ColumnSelections?.ContainsKey(column) ?? false) && ColumnSelections[column];

    private FilterSetOffcanvas? FilterSetOffcanvas { get; set; }

    private HxOffcanvas? TableInfoOffcanvas { get; set; }

    private SelectColumnsOffcanvas? SelectColumnsOffcanvas { get; set; }

    private bool EditModeEnabled { get; set; } = true;

    private bool Exporting { get; set; } = false;

    private bool DiscardChanges { get; set; } = false; // used to prevent double confirmation when switching tables

    protected override async Task OnParametersSetAsync()
    {
        if (TableId is not null && TableId != SelectedTable?.DataTableId)
        {
            var table = Tables?.FirstOrDefault(t => t.DataTableId == TableId);
            await ReloadDataAsync(table);
        }
    }

    private List<Row>? GetOrderedRowRecords()
    {
        var rows = TableData?.Rows;
        foreach (var orderBy in OrderBy)
        {
            if (orderBy.Descending)
            {
                rows = rows?.OrderByDescending(r => r.Values[orderBy.Column]);
            }
            else
            {
                rows = rows?.OrderBy(r => r.Values[orderBy.Column]);
            }
        }
        return rows?.ToList();
    }

    private void ToggleOrderBy(string column)
    {
        // ascending (false) => descending (true) => removed
        var index = OrderBy.FindIndex(o => o.Column == column);
        if (index >= 0)
        {
            var orderBy = OrderBy[index];
            OrderBy.Remove(orderBy);
            if (!orderBy.Descending)
            {
                OrderBy.Insert(index, (column, true));
            }
        }
        else
        {
            OrderBy.Insert(0, (column, false));
        }
    }

    private async Task ReloadDataAsync(MasterDataTable? table = null)
    {
        if (TableData?.HasChanges == true && !DiscardChanges)
        {
            var confirmed = await MessageBox.ConfirmAsync("Discard unsaved changes?");
            if (!confirmed)
            {
                return;
            }
        }
        DiscardChanges = false;
        TableData = null;
        StateHasChanged();

        if (table is not null)
        {
            OrderBy.Clear();
            FilterSet = null;
            ColumnSelections = null;
            SelectedTable = table;
        }

        if (SelectedTable is null)
        {
            Messenger.AddError("Error loading data", $"Selected table was null.");
            return;
        }

        try
        {
            TableData = await SelectedTable.LoadDataAsync(TopRows, FilterSet);
            FilterSet ??= TableData.EmptyFilterSet;
            ColumnSelections ??= TableData.Columns.ToDictionary(x => x.Name, x => true);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error loading data", ex.Message);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (TableData is null)
        {
            Messenger.AddError("Error saving changes", $"Table editor dataset object was null.");
            return;
        }

        try
        {
            var (inserted, updated, deleted) = await TableData.SaveChangesAsync();
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
            TableData = await SelectedTable.LetAsync(x => x.LoadDataAsync(TopRows, FilterSet)) ?? TableData;
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error saving changes", $"Error while committing changes to the database. No changes were made.{System.Environment.NewLine}{ex.Message}");
        }
    }

    private async Task DownloadExportAsync(bool filtered)
    {
        Exporting = true;
        try
        {
            ArgumentNullException.ThrowIfNull(SelectedTable);
            var filterSet = filtered ? FilterSet : null;
            var dataset = await SelectedTable.LoadDataAsync(top: int.MaxValue, filters: filterSet);
            using var stream = dataset.GetExcelExportStream();

            var regexSearch = new string(Path.GetInvalidFileNameChars());
            var regex = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var tableName = SelectedTable is not null ? regex.Replace(SelectedTable.DataTableName, "") : "export";
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
            Exporting = false;
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await MessageBox.ConfirmAsync("Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
        DiscardChanges = true;
    }
}
