using Biflow.Ui.TableEditor;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace Biflow.Ui.Shared.DataTables;

public partial class DataTableEditModal(
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory,
    IJSRuntime js,
    DataTableValidator dataTableValidator) : ComponentBase, IDisposable
{
    [Parameter] public EventCallback<MasterDataTable> OnTableSubmitted { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly IJSRuntime _js = js;
    private readonly DataTableValidator _dataTableValidator = dataTableValidator;
    private readonly ConditionalWeakTable<MasterDataTableLookup, IEnumerable<string>> _lookupColumns = [];

    private HxModal? _modal;
    private DatabaseTableSelectOffcanvas? _offcanvas;
    private AppDbContext? _editContext;
    private MasterDataTable? _editTable;
    private TableEditView _currentView = TableEditView.Settings;
    private IEnumerable<MasterDataTable>? _tables;
    private IEnumerable<MsSqlConnection>? _connections;
    private IEnumerable<MasterDataTableCategory>? _categories;
    private IEnumerable<string>? _columns;
    private string _columnOrderSelected = "";

    private enum TableEditView { Settings, ColumnOrder, Lookups }

    public async Task SetEditContextAsync(MasterDataTable? table = null)
    {
        _currentView = TableEditView.Settings;
        _columnOrderSelected = "";
        await _modal.LetAsync(x => x.ShowAsync());
        if (_editContext is not null) await _editContext.DisposeAsync();
        _editContext = await _dbContextFactory.CreateDbContextAsync();
        _connections = await _editContext.MsSqlConnections
            .OrderBy(c => c.ConnectionName)
            .ToListAsync();
        _categories = await _editContext.MasterDataTableCategories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        _tables = await _editContext.MasterDataTables
            .Include(t => t.Connection)
            .ThenInclude(c => c.Credential)
            .Include(t => t.Category)
            .OrderBy(t => t.Category!.CategoryName)
            .ThenBy(t => t.DataTableName)
            .ToListAsync();
        if (table is null)
        {
            // New table
            _editTable = new()
            {
                Connection = _connections.First(),
                ConnectionId = _connections!.First().ConnectionId
            };
        }
        else
        {
            _editTable = await _editContext.MasterDataTables
                .Include(t => t.Connection)
                .ThenInclude(c => c.Credential)
                .Include(t => t.Lookups)
                .ThenInclude(l => l.LookupTable)
                .FirstAsync(t => t.DataTableId == table.DataTableId);
            _columns = await _editTable.GetColumnNamesAsync();

            foreach (var lookup in _editTable.Lookups)
            {
                _lookupColumns.AddOrUpdate(lookup, await lookup.LookupTable.GetColumnNamesAsync());
            }
        }
        StateHasChanged();
    }

    private void ConnectionChanged(ChangeEventArgs args)
    {
        var guid = Guid.Parse(args.Value!.ToString()!);
        var connection = _connections!.First(c => c.ConnectionId == guid);
        _editTable!.ConnectionId = guid;
        _editTable!.Connection = connection;
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
                _lookupColumns.Remove(lookup);
                return;
            }
            lookup.LookupTableId = dataTable.DataTableId;
            lookup.LookupTable = dataTable;
            _lookupColumns.AddOrUpdate(lookup, await lookup.LookupTable.GetColumnNamesAsync());
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error selecting lookup table", ex.Message);
        }
    }

    private async Task EndEditContext()
    {
        await _modal.LetAsync(x => x.HideAsync());
        if (_editContext is not null) await _editContext.DisposeAsync();
        await Task.Delay(500);
        _editTable = null;
        _columns = null;
        StateHasChanged();
    }

    private Task<AutosuggestDataProviderResult<string>> GetColumnSuggestions(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<string>
        {
            Data = _columns?.Where(c => c.ContainsIgnoreCase(request.UserInput)) ?? []
        });
    }

    private Task<InputTagsDataProviderResult> GetLockedColumnSuggestions(InputTagsDataProviderRequest request)
    {
        return Task.FromResult(new InputTagsDataProviderResult
        {
            Data = _columns?.Where(c => c.ContainsIgnoreCase(request.UserInput) && (!_editTable?.LockedColumns.Contains(c) ?? true))
                ?? []
        });
    }

    private Task<InputTagsDataProviderResult> GetHiddenColumnSuggestions(InputTagsDataProviderRequest request)
    {
        return Task.FromResult(new InputTagsDataProviderResult
        {
            Data = _columns?.Where(c => c.ContainsIgnoreCase(request.UserInput) && (!_editTable?.HiddenColumns.Contains(c) ?? true))
                ?? []
        });
    }

    private Task<AutosuggestDataProviderResult<MasterDataTable>> GetLookupTableSuggestions(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<MasterDataTable>
        {
            Data = _tables
                ?.Where(t => t.DataTableName.ContainsIgnoreCase(request.UserInput) || (t.Category?.CategoryName.ContainsIgnoreCase(request.UserInput) ?? false))
                ?? []
        });
    }

    private Task<AutosuggestDataProviderResult<string>> GetLookupColumnSuggestions(AutosuggestDataProviderRequest request, MasterDataTableLookup lookup)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<string>
        {
            Data = _lookupColumns.GetValueOrDefault(lookup)?.Where(c => c.ContainsIgnoreCase(request.UserInput)) ?? []
        });
    }

    private async Task SubmitEditTableAsync()
    {
        if (_editContext is null || _editTable is null)
        {
            return;
        }

        if (_editTable.Lookups.Any(lookup => lookup.LookupTable.ConnectionId != _editTable.ConnectionId))
        {
            _toaster.AddError("Validation error", "All lookup tables must use the same connection as the main table.");
            return;
        }

        try
        {
            if (_editTable.DataTableId == Guid.Empty)
            {
                _editContext.MasterDataTables.Add(_editTable);
            }
            else
            {
                // Force update of ColumnOrder as EF may not consider it modified if items only change places.
                _editContext.Entry(_editTable).Property(p => p.ColumnOrder).IsModified = true;
            }
            await _editContext.SaveChangesAsync();
            await OnTableSubmitted.InvokeAsync(_editTable);
            await EndEditContext();
        }
        catch (DbUpdateConcurrencyException)
        {
            _toaster.AddError("Concurrency error", "The data table was modified outside of this session. Reload the page to view the most recent values.");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error updating data table", ex.Message);
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await _js.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private async Task ImportColumnOrderColumnsAsync()
    {
        ArgumentNullException.ThrowIfNull(_editTable);
        _columns = await _editTable.GetColumnNamesAsync();
        if (!_columns.Any())
        {
            _toaster.AddWarning($"No columns found for table [{_editTable.TargetSchemaName}].[{_editTable.TargetTableName}]");
            return;
        }
        _editTable.ColumnOrder.Clear();
        _editTable.ColumnOrder.AddRange(_columns);
    }

    private void DemoteSelectedColumn()
    {
        ArgumentNullException.ThrowIfNull(_editTable);
        var oldIndex = _editTable.ColumnOrder.IndexOf(_columnOrderSelected);
        if (oldIndex < 0 || oldIndex >= _editTable.ColumnOrder.Count - 1)
        {
            // Cannot demote any more.
            return;
        }
        _editTable.ColumnOrder.RemoveAt(oldIndex);
        _editTable.ColumnOrder.Insert(oldIndex + 1, _columnOrderSelected);
    }

    private void PromoteSelectedColumn()
    {
        ArgumentNullException.ThrowIfNull(_editTable);
        var oldIndex = _editTable.ColumnOrder.IndexOf(_columnOrderSelected);
        if (oldIndex < 1)
        {
            // Cannot promote any more.
            return;
        }
        _editTable.ColumnOrder.RemoveAt(oldIndex);
        _editTable.ColumnOrder.Insert(oldIndex - 1, _columnOrderSelected);
    }

    private async Task OnTableSelected(DatabaseTableSelectedResult table)
    {
        if (_editTable is null) return;
        (_editTable.TargetSchemaName, _editTable.TargetTableName) = table;
        _columns = await _editTable.GetColumnNamesAsync();
    }

    public void Dispose() => _editContext?.Dispose();
}
