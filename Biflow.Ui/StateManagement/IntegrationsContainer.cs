namespace Biflow.Ui.StateManagement;

public record IntegrationsContainer
{
    public static readonly IntegrationsContainer Empty = new()
    {
        SqlConnections = [],
        MsSqlConnections = [],
        AnalysisServicesConnections = [],
        PipelineClients = [],
        AzureCredentials = [],
        FunctionApps = [],
        QlikCloudClients = [],
        DatabricksWorkspaces = [],
        DbtAccounts = [],
        ScdTables = [],
        Credentials = [],
        Proxies = []
    };
    
    public required List<SqlConnectionBase> SqlConnections { get; init; }
    public required List<MsSqlConnection> MsSqlConnections { get; init; }
    public required List<AnalysisServicesConnection> AnalysisServicesConnections { get; init; }
    public required List<PipelineClient> PipelineClients { get; init; }
    public required List<AzureCredential> AzureCredentials { get; init; }
    public required List<FunctionApp> FunctionApps { get; init; }
    public required List<QlikCloudEnvironment> QlikCloudClients { get; init; }
    public required List<DatabricksWorkspace> DatabricksWorkspaces { get; init; }
    public required List<DbtAccount> DbtAccounts { get; init; }
    public required List<ScdTable> ScdTables { get; init; }
    public required List<Credential> Credentials { get; init; }
    public required List<Proxy> Proxies { get; init; }
}