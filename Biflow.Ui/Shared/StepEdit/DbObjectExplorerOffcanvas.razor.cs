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

    private IEnumerable<DbObject>? DatabaseObjects { get; set; }

    private string SchemaSearchTerm { get; set; } = string.Empty;

    private string NameSearchTerm { get; set; } = string.Empty;

    private CancellationTokenSource Cts { get; set; } = new();

    private bool Loading => QueryTask is not null;

    private SemaphoreSlim Semaphore { get; } = new(1, 1);

    private Task<IEnumerable<DbObject>>? QueryTask { get; set; }

    private async Task RunQueryAsync()
    {
        if (SchemaSearchTerm.Length < 2 && NameSearchTerm.Length < 2)
        {
            return;
        }

        // If another query task is alerady running, cancel it, wait for its completion and continue with a new query.
        if (QueryTask is not null && !Cts.IsCancellationRequested)
        {
            Cts.Cancel();
        }

        try
        {
            await Semaphore.WaitAsync();
            Guid connectionId = ConnectionId ?? throw new ArgumentNullException(nameof(ConnectionId), "Connection id was null");
            QueryTask = SqlServerHelper.GetDatabaseObjectsAsync(connectionId, SchemaSearchTerm, NameSearchTerm, 50, Cts.Token);
            StateHasChanged();
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
        finally
        {
            QueryTask = null;
            Semaphore.Release();
        }
    }

    private async Task CloseAsync()
    {
        if (QueryTask is not null && !Cts.IsCancellationRequested)
        {
            Cts.Cancel();
            Cts = new();
        }
        await Offcanvas.LetAsync(x => x.HideAsync());
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
        DatabaseObjects = null;
        SchemaSearchTerm = "";
        NameSearchTerm = "";
        ConnectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        await Offcanvas.LetAsync(x => x.ShowAsync());
    }

    public void Dispose()
    {
        Cts.Dispose();
    }
}
