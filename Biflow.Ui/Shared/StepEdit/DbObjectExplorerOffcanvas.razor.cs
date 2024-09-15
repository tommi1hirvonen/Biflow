using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEdit;

public partial class DbObjectExplorerOffcanvas : ComponentBase, IDisposable
{
    [Inject] public ToasterService Toaster { get; set; } = null!;

    [Parameter] public IEnumerable<ConnectionBase> Connections { get; set; } = [];

    [Parameter] public Action<(string, string, string, string), bool>? OnDbObjectSelected { get; set; }

    private readonly SemaphoreSlim semaphore = new(1, 1);

    private Guid? connectionId;
    private HxOffcanvas? offcanvas;
    private IEnumerable<DbObject>? databaseObjects;
    private string schemaSearchTerm = string.Empty;
    private string nameSearchTerm = string.Empty;
    private CancellationTokenSource cts = new();
    private Task<IEnumerable<DbObject>>? queryTask;

    private bool Loading => queryTask is not null;

    private async Task RunQueryAsync()
    {
        if (schemaSearchTerm.Length < 2 && nameSearchTerm.Length < 2)
        {
            return;
        }

        // If another query task is alerady running, cancel it, wait for its completion and continue with a new query.
        if (queryTask is not null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
        }

        try
        {
            await semaphore.WaitAsync();
            Guid connectionId = this.connectionId ?? throw new ArgumentNullException(nameof(connectionId), "Connection id was null");
            var connection = Connections.First(c => c.ConnectionId == connectionId);
            queryTask = connection switch
            {
                MsSqlConnection ms => ms.GetDatabaseObjectsAsync(schemaSearchTerm, nameSearchTerm, 50, cts.Token),
                SnowflakeConnection sf => sf.GetDatabaseObjectsAsync(schemaSearchTerm, nameSearchTerm, 50, cts.Token),
                _ => throw new ArgumentException($"Unsupported connection type {connection.GetType().Name}")
            };
            StateHasChanged();
            databaseObjects = await queryTask;
        }
        catch (OperationCanceledException)
        {
            cts.Dispose();
            cts = new();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error querying database objects", ex.Message);
        }
        finally
        {
            queryTask = null;
            semaphore.Release();
        }
    }

    private async Task CloseAsync()
    {
        if (queryTask is not null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            cts = new();
        }
        await offcanvas.LetAsync(x => x.HideAsync());
    }

    private async Task SelectDbObjectAsync((string Server, string Database, string Schema, string Name) dbObject, bool commit)
    {
        await offcanvas.LetAsync(x => x.HideAsync());
        if (OnDbObjectSelected is not null)
        {
            OnDbObjectSelected(dbObject, commit);
        }
    }

    public async Task ShowAsync(Guid? connectionId)
    {
        databaseObjects = null;
        schemaSearchTerm = "";
        nameSearchTerm = "";
        this.connectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        await offcanvas.LetAsync(x => x.ShowAsync());
    }

    public void Dispose()
    {
        cts.Dispose();
    }
}
