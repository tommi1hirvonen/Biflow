using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEdit;

public partial class SqlReferenceExplorerOffcanvas : ComponentBase
{
    [Parameter] public IEnumerable<MsSqlConnection> Connections { get; set; } = [];

    private Guid? _connectionId;
    private HxOffcanvas? _offcanvas;
    private string _referencingSchemaOperator = "like";
    private string _referencingSchemaFilter = string.Empty;
    private string _referencingNameOperator = "like";
    private string _referencingNameFilter = string.Empty;
    private string _referencedSchemaOperator = "like";
    private string _referencedSchemaFilter = string.Empty;
    private string _referencedNameOperator = "like";
    private string _referencedNameFilter = string.Empty;
    private string _referencingTypeFilter = string.Empty;
    private string _referencedTypeFilter = string.Empty;

    private IEnumerable<SqlReference> _queryResults = [];

    private IEnumerable<SqlReference> FilteredQueryResults => _queryResults
        .Where(r => _referencingTypeFilter.Length == 0 || r.ReferencingType == _referencingTypeFilter)
        .Where(r => _referencedTypeFilter.Length == 0 || r.ReferencedType == _referencedTypeFilter);

    private async Task RunQueryAsync()
    {
        try
        {
            Guid connectionId = _connectionId ?? throw new ArgumentNullException(nameof(connectionId), "Connection id was null");
            var connection = Connections.First(c => c.ConnectionId == connectionId);
            _queryResults = await connection.GetSqlReferencedObjectsAsync(
                referencingSchemaOperator: _referencingSchemaOperator,
                referencingSchemaFilter: _referencingSchemaFilter,
                referencingNameOperator: _referencingNameOperator,
                referencingNameFilter: _referencingNameFilter,
                referencedSchemaOperator: _referencedSchemaOperator,
                referencedSchemaFilter: _referencedSchemaFilter,
                referencedNameOperator: _referencedNameOperator,
                referencedNameFilter: _referencedNameFilter);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error querying references", ex.Message);
        }

    }

    private async Task NavigateReferencingObjectAsync(SqlReference reference)
    {
        _referencingSchemaFilter = reference.ReferencingSchema;
        _referencingSchemaOperator = "=";

        _referencingNameFilter = reference.ReferencingName;
        _referencingNameOperator = "=";

        _referencedSchemaFilter = string.Empty;
        _referencedSchemaOperator = "like";
        _referencedNameFilter = string.Empty;
        _referencedNameOperator = "like";
        await RunQueryAsync();
    }

    private async Task NavigateReferencedObjectAsync(SqlReference reference)
    {
        _referencedSchemaFilter = reference.ReferencedSchema;
        _referencedSchemaOperator = "=";

        _referencedNameFilter = reference.ReferencedName;
        _referencedNameOperator = "=";

        _referencingSchemaFilter = string.Empty;
        _referencingSchemaOperator = "like";
        _referencingNameFilter = string.Empty;
        _referencingNameOperator = "like";
        await RunQueryAsync();
    }

    private void ClearFilters()
    {
        _referencingSchemaOperator = "like";
        _referencingSchemaFilter = string.Empty;

        _referencingNameOperator = "like";
        _referencingNameFilter = string.Empty;

        _referencedSchemaOperator = "like";
        _referencedSchemaFilter = string.Empty;

        _referencedNameOperator = "like";
        _referencedNameFilter = string.Empty;

        _referencingTypeFilter = string.Empty;
        _referencedTypeFilter = string.Empty;
    }

    public async Task ShowAsync(Guid? connectionId, string? sqlStatement = null)
    {
        _connectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        var proc = sqlStatement is not null ? MsSqlExtensions.ParseStoredProcedureFromSqlStatement(sqlStatement) : null;
        var schema = proc?.Schema;
        var name = proc?.ProcedureName;
        if (schema is not null)
        {
            _referencingSchemaFilter = schema;
            _referencingSchemaOperator = "=";
        }
        if (name is not null)
        {
            _referencingNameFilter = name;
            _referencingNameOperator = "=";
        }
        await _offcanvas.LetAsync(x => x.ShowAsync());

        // Only run the query automatically when opening the modal if some kind of filter was set.
        // Running a query without filters should only be done on demand.
        if (!string.IsNullOrEmpty(_referencingSchemaFilter) || !string.IsNullOrEmpty(_referencingNameFilter))
        {
            await RunQueryAsync();
        }
    }
}
