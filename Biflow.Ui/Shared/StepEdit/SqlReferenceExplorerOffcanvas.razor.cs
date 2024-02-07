using Biflow.Ui.SqlServer;

namespace Biflow.Ui.Shared.StepEdit;

public partial class SqlReferenceExplorerOffcanvas : ComponentBase
{
    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    private Guid? connectionId;
    private HxOffcanvas? offcanvas;
    private string referencingSchemaOperator = "like";
    private string referencingSchemaFilter = string.Empty;
    private string referencingNameOperator = "like";
    private string referencingNameFilter = string.Empty;
    private string referencedSchemaOperator = "like";
    private string referencedSchemaFilter = string.Empty;
    private string referencedNameOperator = "like";
    private string referencedNameFilter = string.Empty;
    private string referencingTypeFilter = string.Empty;
    private string referencedTypeFilter = string.Empty;

    private IEnumerable<SqlReference> queryResults = [];

    private IEnumerable<SqlReference> FilteredQueryResults => queryResults
        .Where(r => referencingTypeFilter.Length == 0 || r.ReferencingType == referencingTypeFilter)
        .Where(r => referencedTypeFilter.Length == 0 || r.ReferencedType == referencedTypeFilter);

    private async Task RunQueryAsync()
    {
        try
        {
            Guid connectionId = this.connectionId ?? throw new ArgumentNullException(nameof(connectionId), "Connection id was null");
            queryResults = await SqlServerHelper.GetSqlReferencedObjectsAsync(
                connectionId,
                referencingSchemaOperator: referencingSchemaOperator,
                referencingSchemaFilter: referencingSchemaFilter,
                referencingNameOperator: referencingNameOperator,
                referencingNameFilter: referencingNameFilter,
                referencedSchemaOperator: referencedSchemaOperator,
                referencedSchemaFilter: referencedSchemaFilter,
                referencedNameOperator: referencedNameOperator,
                referencedNameFilter: referencedNameFilter);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error querying references", ex.Message);
        }

    }

    private async Task NavigateReferencingObjectAsync(SqlReference reference)
    {
        referencingSchemaFilter = reference.ReferencingSchema;
        referencingSchemaOperator = "=";

        referencingNameFilter = reference.ReferencingName;
        referencingNameOperator = "=";

        referencedSchemaFilter = string.Empty;
        referencedSchemaOperator = "like";
        referencedNameFilter = string.Empty;
        referencedNameOperator = "like";
        await RunQueryAsync();
    }

    private async Task NavigateReferencedObjectAsync(SqlReference reference)
    {
        referencedSchemaFilter = reference.ReferencedSchema;
        referencedSchemaOperator = "=";

        referencedNameFilter = reference.ReferencedName;
        referencedNameOperator = "=";

        referencingSchemaFilter = string.Empty;
        referencingSchemaOperator = "like";
        referencingNameFilter = string.Empty;
        referencingNameOperator = "like";
        await RunQueryAsync();
    }

    private void ClearFilters()
    {
        referencingSchemaOperator = "like";
        referencingSchemaFilter = string.Empty;

        referencingNameOperator = "like";
        referencingNameFilter = string.Empty;

        referencedSchemaOperator = "like";
        referencedSchemaFilter = string.Empty;

        referencedNameOperator = "like";
        referencedNameFilter = string.Empty;

        referencingTypeFilter = string.Empty;
        referencedTypeFilter = string.Empty;
    }

    public async Task ShowAsync(Guid? connectionId, string? sqlStatement = null)
    {
        this.connectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        var proc = sqlStatement?.ParseStoredProcedureFromSqlStatement();
        var schema = proc?.Schema;
        var name = proc?.ProcedureName;
        if (schema is not null)
        {
            referencingSchemaFilter = schema;
            referencingSchemaOperator = "=";
        }
        if (name is not null)
        {
            referencingNameFilter = name;
            referencingNameOperator = "=";
        }
        await offcanvas.LetAsync(x => x.ShowAsync());

        // Only run the query automatically when opening the modal if some kind of a filter was set.
        // Running a query without filters should only be done on demand.
        if (!string.IsNullOrEmpty(referencingSchemaFilter) || !string.IsNullOrEmpty(referencingNameFilter))
        {
            await RunQueryAsync();
        }
    }
}
