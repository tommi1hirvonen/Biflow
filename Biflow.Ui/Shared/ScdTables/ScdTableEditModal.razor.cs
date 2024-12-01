using Biflow.Core.Entities.Scd;
using Biflow.Ui.Core.Validation;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.ScdTables;

public partial class ScdTableEditModal(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ScdTableValidator scdTableValidator,
    ToasterService toaster) : ComponentBase
{
    [Parameter]
    public EventCallback<ScdTable> OnTableSubmit { get; set; }
    
    [Parameter]
    public IEnumerable<ConnectionBase>? Connections { get; set; }
    
    private HxModal? _modal;
    private CodeEditor? _preLoadScriptEditor;
    private CodeEditor? _postLoadScriptEditor;
    private View _view = View.Settings;
    private TableSelectOffcanvas? _offcanvas;
    private IEnumerable<FullColumnMetadata>? _columns;
    private bool _firstColumnImport = true;
    private bool _loadingColumns = false;
    private ScdTable? _table;
    private Guid _prevConnectionId;
    // Cache the configurations to fields.
    // This way settings aren't lost if the user changes the table's configuration.
    private SchemaDriftDisabledConfiguration _disabledConfiguration = new();
    private SchemaDriftEnabledConfiguration _enabledConfiguration = new();
    
    private enum View { Settings, PreLoadScript, PostLoadScript }
    
    public async Task ShowAsync(Guid? scdTableId)
    {
        _table = null;
        _columns = null;
        _prevConnectionId = Guid.Empty;
        _firstColumnImport = true;
        _disabledConfiguration = new();
        _enabledConfiguration = new();
        await _modal.LetAsync(x => x.ShowAsync());
        if (scdTableId is { } id)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            _table = await context.ScdTables
                .AsNoTrackingWithIdentityResolution()
                .Include(t => t.Connection)
                .FirstAsync(t => t.ScdTableId == id);
            _prevConnectionId = _table.ConnectionId;
            StateHasChanged();
            switch (_table.SchemaDriftConfiguration)
            {
                case SchemaDriftDisabledConfiguration disabled:
                    _disabledConfiguration = disabled;
                    _columns = await LoadColumnsAsync();
                    var excludedColumns = _columns
                        .Select(c => c.ColumnName)
                        .Where(c1 => disabled.IncludedColumns.Concat(_table.NaturalKeyColumns).All(c2 => c1 != c2))
                        .Order();
                    _enabledConfiguration.ExcludedColumns.AddRange(excludedColumns);
                    break;
                case SchemaDriftEnabledConfiguration enabled:
                    _enabledConfiguration = enabled;
                    _columns = await LoadColumnsAsync();
                    var includedColumns = _columns
                        .Select(c => c.ColumnName)
                        .Where(c1 => enabled.ExcludedColumns.All(c2 => c1 != c2))
                        .Order(); 
                    _disabledConfiguration.IncludedColumns.AddRange(includedColumns);
                    break;
            }
        }
        else
        {
            _columns = [];
            ArgumentNullException.ThrowIfNull(Connections);
            var connection = Connections.FirstOrDefault();
            ArgumentNullException.ThrowIfNull(connection);
            _prevConnectionId = connection.ConnectionId;
            _table = new()
            {
                ConnectionId = connection.ConnectionId,
                SchemaDriftConfiguration = _disabledConfiguration
            };
        }
    }

    private async Task ShowTablesOffcanvasAsync()
    {
        ArgumentNullException.ThrowIfNull(_table);
        ArgumentNullException.ThrowIfNull(Connections);
        var connection = Connections.FirstOrDefault(c => c.ConnectionId == _table.ConnectionId);
        ArgumentNullException.ThrowIfNull(connection);
        await _offcanvas.LetAsync(x => x.ShowAsync(connection));
    }
    
    private async Task OnTableSelected(DbObject table)
    {
        ArgumentNullException.ThrowIfNull(_table);
        if (_table.SourceTableSchema != table.Schema || _table.SourceTableName != table.Object)
        {
            // Table was changed. Reset _firstColumnImport.
            _firstColumnImport = true;
        }
        (_table.SourceTableSchema, _table.SourceTableName) = (table.Schema, table.Object);
        if (string.IsNullOrEmpty(_table.TargetTableSchema) && string.IsNullOrEmpty(_table.TargetTableName))
        {
            // TODO: Handle target table name conventions dynamically based on preferences set on the connection.
            _table.TargetTableSchema = _table.SourceTableSchema;
            _table.TargetTableName = $"{_table.SourceTableName}_SCD";
        }
        if (string.IsNullOrEmpty(_table.StagingTableSchema) && string.IsNullOrEmpty(_table.StagingTableName))
        {
            // TODO: Handle staging table name conventions dynamically based on preferences set on the connection.
            _table.StagingTableSchema = _table.SourceTableSchema;
            _table.StagingTableName = $"{_table.SourceTableName}_SCD_DELTA";
        }
        _columns = await LoadColumnsAsync();
    }

    private async Task<IEnumerable<FullColumnMetadata>> LoadColumnsAsync()
    {
        try
        {
            _loadingColumns = true;
            ArgumentNullException.ThrowIfNull(_table);
            ArgumentNullException.ThrowIfNull(Connections);
            var connection = Connections.FirstOrDefault(c => c.ConnectionId == _table.ConnectionId);
            ArgumentNullException.ThrowIfNull(connection);
            var columnProvider = connection.CreateColumnMetadataProvider();
            var columns = await columnProvider.GetColumnsAsync(
                _table.SourceTableSchema,
                _table.SourceTableName);
            if (_table.ScdTableId == Guid.Empty && _firstColumnImport)
            {
                // New table and columns are being imported for the first time.
                _disabledConfiguration.IncludedColumns.Clear();
                _disabledConfiguration.IncludedColumns.AddRange(columns.Select(c => c.ColumnName).Order());
            }

            _firstColumnImport = false;
            _prevConnectionId = connection.ConnectionId;
            return columns;
        }
        catch (Exception ex)
        {
            toaster.AddError("Error importing columns", ex.Message);
            return [];
        }
        finally
        {
            _loadingColumns = false;
        }
    }
    
    private async Task SubmitAsync()
    {
        ArgumentNullException.ThrowIfNull(_table);
        _table.NaturalKeyColumns.Sort();
        // Included columns should not contain natural key columns.
        // Natural key columns are included by default.
        _disabledConfiguration.IncludedColumns.RemoveAll(c => _table.NaturalKeyColumns.Contains(c));
        _disabledConfiguration.IncludedColumns.Sort();
        // Excluded columns should not contain natural key columns.
        // Natural key columns cannot be excluded.
        _enabledConfiguration.ExcludedColumns.RemoveAll(c => _table.NaturalKeyColumns.Contains(c));
        _enabledConfiguration.ExcludedColumns.Sort();
        await OnTableSubmit.InvokeAsync(_table);
        await _modal.LetAsync(x => x.HideAsync());
    }
}