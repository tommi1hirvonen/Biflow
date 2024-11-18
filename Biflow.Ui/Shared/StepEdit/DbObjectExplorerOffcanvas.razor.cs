using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEdit;

public partial class DbObjectExplorerOffcanvas(ToasterService toaster) : ComponentBase, IDisposable
{
    [Parameter] public IEnumerable<ConnectionBase> Connections { get; set; } = [];

    [Parameter] public Action<(string, string, string, string), bool>? OnDbObjectSelected { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private Guid? _connectionId;
    private HxOffcanvas? _offcanvas;
    private IEnumerable<DbObject>? _databaseObjects;
    private string _schemaSearchTerm = string.Empty;
    private string _nameSearchTerm = string.Empty;
    private CancellationTokenSource _cts = new();
    private Task<IEnumerable<DbObject>>? _queryTask;

    private bool Loading => _queryTask is not null;

    private async Task RunQueryAsync()
    {
        if (_schemaSearchTerm.Length < 2 && _nameSearchTerm.Length < 2)
        {
            return;
        }

        // If another query task is already running, cancel it, wait for its completion and continue with a new query.
        if (_queryTask is not null && !_cts.IsCancellationRequested)
        {
            await _cts.CancelAsync();
        }

        try
        {
            await _semaphore.WaitAsync();
            Guid connectionId = _connectionId ?? throw new ArgumentNullException(nameof(connectionId), "Connection id was null");
            var connection = Connections.First(c => c.ConnectionId == connectionId);
            _queryTask = connection switch
            {
                MsSqlConnection ms => ms.GetDatabaseObjectsAsync(_schemaSearchTerm, _nameSearchTerm, 50, _cts.Token),
                SnowflakeConnection sf => sf.GetDatabaseObjectsAsync(_schemaSearchTerm, _nameSearchTerm, 50, _cts.Token),
                _ => throw new ArgumentException($"Unsupported connection type {connection.GetType().Name}")
            };
            StateHasChanged();
            _databaseObjects = await _queryTask;
        }
        catch (OperationCanceledException)
        {
            _cts.Dispose();
            _cts = new();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error querying database objects", ex.Message);
        }
        finally
        {
            _queryTask = null;
            _semaphore.Release();
        }
    }

    private async Task CloseAsync()
    {
        if (_queryTask is not null && !_cts.IsCancellationRequested)
        {
            await _cts.CancelAsync();
            _cts = new();
        }
        await _offcanvas.LetAsync(x => x.HideAsync());
    }

    private async Task SelectDbObjectAsync((string Server, string Database, string Schema, string Name) dbObject, bool commit)
    {
        await _offcanvas.LetAsync(x => x.HideAsync());
        OnDbObjectSelected?.Invoke(dbObject, commit);
    }

    public async Task ShowAsync(Guid? connectionId)
    {
        _databaseObjects = null;
        _schemaSearchTerm = "";
        _nameSearchTerm = "";
        _connectionId = connectionId ?? Connections.FirstOrDefault()?.ConnectionId;
        await _offcanvas.LetAsync(x => x.ShowAsync());
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
