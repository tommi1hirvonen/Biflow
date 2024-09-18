using Biflow.Ui.TableEditor;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace Biflow.Ui.Shared.DataTables;

public partial class DataTableEditModal : ComponentBase, IDisposable
{
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private DataTableValidator DataTableValidator { get; set; } = null!;

    [Parameter] public EventCallback<MasterDataTable> OnTableSubmitted { get; set; }

    private readonly ConditionalWeakTable<MasterDataTableLookup, IEnumerable<string>> lookupColumns = [];

    private HxModal? modal;
    private DatabaseTableSelectOffcanvas? offcanvas;
    private AppDbContext? editContext;
    private MasterDataTable? editTable;
    private TableEditView currentView = TableEditView.Settings;
    private IEnumerable<MasterDataTable>? tables;
    private IEnumerable<MsSqlConnection>? connections;
    private IEnumerable<MasterDataTableCategory>? categories;
    private IEnumerable<string>? columns;
    private string columnOrderSelected = "";

    private enum TableEditView { Settings, ColumnOrder, Lookups }

    public async Task SetEditContextAsync(MasterDataTable? table = null)
    {
        currentView = TableEditView.Settings;
        columnOrderSelected = "";
        await modal.LetAsync(x => x.ShowAsync());
        editContext?.Dispose();
        editContext = DbContextFactory.CreateDbContext();
        connections = await editContext.MsSqlConnections
            .OrderBy(c => c.ConnectionName)
            .ToListAsync();
        categories = await editContext.MasterDataTableCategories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        tables = await editContext.MasterDataTables
            .Include(t => t.Connection)
            .ThenInclude(c => c.Credential)
            .Include(t => t.Category)
            .OrderBy(t => t.Category!.CategoryName)
            .ThenBy(t => t.DataTableName)
            .ToListAsync();
        if (table is null)
        {
            // New table
            editTable = new()
            {
                Connection = connections.First(),
                ConnectionId = connections!.First().ConnectionId
            };
        }
        else
        {
            editTable = await editContext.MasterDataTables
                .Include(t => t.Connection)
                .ThenInclude(c => c.Credential)
                .Include(t => t.Lookups)
                .ThenInclude(l => l.LookupTable)
                .FirstAsync(t => t.DataTableId == table.DataTableId);
            columns = await editTable.GetColumnNamesAsync();

            foreach (var lookup in editTable.Lookups)
            {
                lookupColumns.AddOrUpdate(lookup, await lookup.LookupTable.GetColumnNamesAsync());
            }
        }
        StateHasChanged();
    }

    private void ConnectionChanged(ChangeEventArgs args)
    {
        var guid = Guid.Parse(args.Value!.ToString()!);
        var connection = connections!.First(c => c.ConnectionId == guid);
        editTable!.ConnectionId = guid;
        editTable!.Connection = connection;
    }

    private async void SetLookupTable(MasterDataTable? dataTable, MasterDataTableLookup lookup)
    {
        try
        {
            lookup.LookupValueColumn = "";
            lookup.LookupDescriptionColumn = "";
            if (dataTable is null)
            {
                lookup.LookupTable = null!;
                lookup.LookupTableId = Guid.Empty;
                lookupColumns.Remove(lookup);
                return;
            }
            lookup.LookupTableId = dataTable.DataTableId;
            lookup.LookupTable = dataTable;
            lookupColumns.AddOrUpdate(lookup, await lookup.LookupTable.GetColumnNamesAsync());
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error selecting lookup table", ex.Message);
        }
    }

    private async Task EndEditContext()
    {
        await modal.LetAsync(x => x.HideAsync());
        editContext?.Dispose();
        await Task.Delay(500);
        editTable = null;
        columns = null;
        StateHasChanged();
    }

    private Task<AutosuggestDataProviderResult<string>> GetColumnSuggestions(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<string>
        {
            Data = columns?.Where(c => c.ContainsIgnoreCase(request.UserInput)) ?? []
        });
    }

    private Task<InputTagsDataProviderResult> GetLockedColumnSuggestions(InputTagsDataProviderRequest request)
    {
        return Task.FromResult(new InputTagsDataProviderResult
        {
            Data = columns?.Where(c => c.ContainsIgnoreCase(request.UserInput) && (!editTable?.LockedColumns.Contains(c) ?? true))
                ?? []
        });
    }

    private Task<InputTagsDataProviderResult> GetHiddenColumnSuggestions(InputTagsDataProviderRequest request)
    {
        return Task.FromResult(new InputTagsDataProviderResult
        {
            Data = columns?.Where(c => c.ContainsIgnoreCase(request.UserInput) && (!editTable?.HiddenColumns.Contains(c) ?? true))
                ?? []
        });
    }

    private Task<AutosuggestDataProviderResult<MasterDataTable>> GetLookupTableSuggestions(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<MasterDataTable>
        {
            Data = tables
                ?.Where(t => t.DataTableName.ContainsIgnoreCase(request.UserInput) || (t.Category?.CategoryName.ContainsIgnoreCase(request.UserInput) ?? false))
                ?? []
        });
    }

    private Task<AutosuggestDataProviderResult<string>> GetLookupColumnSuggestions(AutosuggestDataProviderRequest request, MasterDataTableLookup lookup)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<string>
        {
            Data = lookupColumns.GetValueOrDefault(lookup)?.Where(c => c.ContainsIgnoreCase(request.UserInput)) ?? []
        });
    }

    private async Task SubmitEditTableAsync()
    {
        if (editContext is null || editTable is null)
        {
            return;
        }

        if (editTable.Lookups.Any(lookup => lookup.LookupTable.ConnectionId != editTable.ConnectionId))
        {
            Toaster.AddError("Validation error", "All lookup tables must use the same connection as the main table.");
            return;
        }

        try
        {
            if (editTable.DataTableId == Guid.Empty)
            {
                editContext.MasterDataTables.Add(editTable);
            }
            else
            {
                // Force update of ColumnOrder as EF may not consider it modified if items only change places.
                editContext.Entry(editTable).Property(p => p.ColumnOrder).IsModified = true;
            }
            await editContext.SaveChangesAsync();
            await OnTableSubmitted.InvokeAsync(editTable);
            await EndEditContext();
        }
        catch (DbUpdateConcurrencyException)
        {
            Toaster.AddError("Concurrency error", "The data table was modified outside of this session. Reload the page to view the most recent values.");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error updating data table", ex.Message);
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private async Task ImportColumnOrderColumnsAsync()
    {
        ArgumentNullException.ThrowIfNull(editTable);
        columns = await editTable.GetColumnNamesAsync();
        if (!columns.Any())
        {
            Toaster.AddWarning($"No columns found for table [{editTable.TargetSchemaName}].[{editTable.TargetTableName}]");
            return;
        }
        editTable.ColumnOrder.Clear();
        editTable.ColumnOrder.AddRange(columns);
    }

    private void DemoteSelectedColumn()
    {
        ArgumentNullException.ThrowIfNull(editTable);
        var oldIndex = editTable.ColumnOrder.IndexOf(columnOrderSelected);
        if (oldIndex >= 0 && oldIndex < editTable.ColumnOrder.Count - 1)
        {
            editTable.ColumnOrder.RemoveAt(oldIndex);
            editTable.ColumnOrder.Insert(oldIndex + 1, columnOrderSelected);
        }
    }

    private void PromoteSelectedColumn()
    {
        ArgumentNullException.ThrowIfNull(editTable);
        var oldIndex = editTable.ColumnOrder.IndexOf(columnOrderSelected);
        if (oldIndex >= 1)
        {
            editTable.ColumnOrder.RemoveAt(oldIndex);
            editTable.ColumnOrder.Insert(oldIndex - 1, columnOrderSelected);
        }
    }

    private async Task OnTableSelected(DatabaseTableSelectedResult table)
    {
        if (editTable is null) return;
        (editTable.TargetSchemaName, editTable.TargetTableName) = table;
        columns = await editTable.GetColumnNamesAsync();
    }

    public void Dispose() => editContext?.Dispose();
}
