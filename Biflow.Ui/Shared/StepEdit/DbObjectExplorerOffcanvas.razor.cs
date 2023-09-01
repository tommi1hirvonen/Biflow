using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;

namespace Biflow.Ui.Shared.StepEdit;

public partial class DbObjectExplorerOffcanvas : ComponentBase, IDisposable
{
    [Inject] public IHxMessengerService Messenger { get; set; } = null!;

    [Inject] public SqlServerHelperService SqlServerHelper { get; set; } = null!;

    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    [Parameter] public Action<(string, string, string, string), bool>? OnDbObjectSelected { get; set; }

    private Guid? ConnectionId { get; set; }

    private HxOffcanvas? Offcanvas { get; set; }

    private IEnumerable<(string Server, string Database, string Schema, string Object, string Type)> DatabaseObjects { get; set; } =
        Enumerable.Empty<(string, string, string, string, string)>();

    private IEnumerable<(string Server, string Database, string Schema, string Object, string Type)> FilteredDatabaseObjects =>
        DatabaseObjects
        .Where(o => o.Schema.ContainsIgnoreCase(SchemaFilter))
        .Where(o => o.Object.ContainsIgnoreCase(ObjectFilter));

    private string SchemaFilter { get; set; } = string.Empty;
    private string ObjectFilter { get; set; } = string.Empty;

    private CancellationTokenSource Cts { get; set; } = new();
    private Task<IEnumerable<(string Server, string Database, string Schema, string Object, string Type)>>? QueryTask { get; set; }

    private async Task RunQueryAsync()
    {
        try
        {
            Guid connectionId = ConnectionId ?? throw new ArgumentNullException(nameof(ConnectionId), "Connection id was null");
            QueryTask = SqlServerHelper.GetDatabaseObjectsAsync(connectionId, Cts.Token);
            DatabaseObjects = await QueryTask;
        }
        catch (OperationCanceledException)
        {
            Cts.Dispose();
            Cts = new();
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error querying database objects", ex.Message);
        }
    }

    private void CancelQuery()
    {
        if (!Cts.IsCancellationRequested)
            Cts.Cancel();
    }

    private async Task SelectDbObjectAsync((string Server, string Database, string Schema, string Name) dbObject, bool commit)
    {
        await Offcanvas.LetAsync(x => x.HideAsync());
        if (OnDbObjectSelected is not null)
        {
            OnDbObjectSelected(dbObject, commit);
        }
    }

    public async Task ShowAsync(Guid? connectionId)
    {
        ConnectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        await Offcanvas.LetAsync(x => x.ShowAsync());
        await RunQueryAsync();
    }

    public void Dispose()
    {
        Cts.Dispose();
    }
}
