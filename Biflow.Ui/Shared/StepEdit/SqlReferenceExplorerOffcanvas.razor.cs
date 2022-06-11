using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;

namespace Biflow.Ui.Shared.StepEdit;

public partial class SqlReferenceExplorerOffcanvas : ComponentBase
{
    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    private Guid? ConnectionId { get; set; }

    private HxOffcanvas Offcanvas { get; set; } = null!;

    private string ReferencingSchemaOperator { get; set; } = "like";
    private string ReferencingSchemaFilter { get; set; } = string.Empty;

    private string ReferencingNameOperator { get; set; } = "like";
    private string ReferencingNameFilter { get; set; } = string.Empty;

    private string ReferencedSchemaOperator { get; set; } = "like";
    private string ReferencedSchemaFilter { get; set; } = string.Empty;

    private string ReferencedNameOperator { get; set; } = "like";
    private string ReferencedNameFilter { get; set; } = string.Empty;

    private string ReferencingTypeFilter { get; set; } = string.Empty;
    private string ReferencedTypeFilter { get; set; } = string.Empty;

    private IEnumerable<SqlReference> FilteredQueryResults => QueryResults
        .Where(r => !ReferencingTypeFilter.Any() || r.ReferencingType == ReferencingTypeFilter)
        .Where(r => !ReferencedTypeFilter.Any() || r.ReferencedType == ReferencedTypeFilter);

    private IEnumerable<SqlReference> QueryResults { get; set; } = Enumerable.Empty<SqlReference>();

    private async Task RunQueryAsync()
    {
        try
        {
            Guid connectionId = ConnectionId ?? throw new ArgumentNullException(nameof(ConnectionId), "Connection id was null");
            QueryResults = await SqlServerHelper.GetSqlReferencedObjectsAsync(
                connectionId,
                referencingSchemaOperator: ReferencingSchemaOperator,
                referencingSchemaFilter: ReferencingSchemaFilter,
                referencingNameOperator: ReferencingNameOperator,
                referencingNameFilter: ReferencingNameFilter,
                referencedSchemaOperator: ReferencedSchemaOperator,
                referencedSchemaFilter: ReferencedSchemaFilter,
                referencedNameOperator: ReferencedNameOperator,
                referencedNameFilter: ReferencedNameFilter);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error querying references", ex.Message);
        }

    }

    private async Task NavigateReferencingObjectAsync(SqlReference reference)
    {
        ReferencingSchemaFilter = reference.ReferencingSchema;
        ReferencingSchemaOperator = "=";

        ReferencingNameFilter = reference.ReferencingName;
        ReferencingNameOperator = "=";

        ReferencedSchemaFilter = string.Empty;
        ReferencedSchemaOperator = "like";
        ReferencedNameFilter = string.Empty;
        ReferencedNameOperator = "like";
        await RunQueryAsync();
    }

    private async Task NavigateReferencedObjectAsync(SqlReference reference)
    {
        ReferencedSchemaFilter = reference.ReferencedSchema;
        ReferencedSchemaOperator = "=";

        ReferencedNameFilter = reference.ReferencedName;
        ReferencedNameOperator = "=";

        ReferencingSchemaFilter = string.Empty;
        ReferencingSchemaOperator = "like";
        ReferencingNameFilter = string.Empty;
        ReferencingNameOperator = "like";
        await RunQueryAsync();
    }

    private void ClearFilters()
    {
        ReferencingSchemaOperator = "like";
        ReferencingSchemaFilter = string.Empty;

        ReferencingNameOperator = "like";
        ReferencingNameFilter = string.Empty;

        ReferencedSchemaOperator = "like";
        ReferencedSchemaFilter = string.Empty;

        ReferencedNameOperator = "like";
        ReferencedNameFilter = string.Empty;

        ReferencingTypeFilter = string.Empty;
        ReferencedTypeFilter = string.Empty;
    }

    public async Task ShowAsync(Guid? connectionId, string? sqlStatement = null)
    {
        ConnectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        var proc = sqlStatement?.ParseStoredProcedureFromSqlStatement();
        var schema = proc?.Schema;
        var name = proc?.ProcedureName;
        if (schema is not null)
        {
            ReferencingSchemaFilter = schema;
            ReferencingSchemaOperator = "=";
        }
        if (name is not null)
        {
            ReferencingNameFilter = name;
            ReferencingNameOperator = "=";
        }
        await Offcanvas.ShowAsync();
        await RunQueryAsync();
    }
}
