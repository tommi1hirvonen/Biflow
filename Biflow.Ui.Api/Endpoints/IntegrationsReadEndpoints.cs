namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class IntegrationsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.IntegrationsRead]);
        
        var group = app.MapGroup("/integrations")
            .WithTags(Scopes.IntegrationsRead)
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("/analysisservicesconnections", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.AnalysisServicesConnections
                    .AsNoTracking()
                    .OrderBy(x => x.ConnectionName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<AnalysisServicesConnection[]>()
            .WithSummary("Get all Analysis Services connections")
            .WithDescription("Get all SQL Server Analysis Services connections. " +
                             "Sensitive connection strings will be replaced with an empty value.")
            .WithName("GetAnalysisServicesConnections");
        
        group.MapGet("/analysisservicesconnections/{connectionId:guid}",
            async (ServiceDbContext dbContext, Guid connectionId, CancellationToken cancellationToken) =>
            {
                var connection = await dbContext.AnalysisServicesConnections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ConnectionId == connectionId, cancellationToken);
                if (connection is null)
                {
                    throw new NotFoundException<AnalysisServicesConnection>(connectionId);
                }
                return Results.Ok(connection);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<AnalysisServicesConnection>()
            .WithSummary("Get Analysis Services connection by id")
            .WithDescription("Get SQL Server Analysis Services connection by id. " +
                             "Sensitive connection string will be replaced with an empty value.")
            .WithName("GetAnalysisServicesConnection");
        
        group.MapGet("/azurecredentials", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.AzureCredentials
                    .AsNoTracking()
                    .OrderBy(x => x.AzureCredentialName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<AzureCredential[]>()
            .WithSummary("Get all Azure credentials")
            .WithDescription("Get all Azure credentials. " +
                             "Sensitive data (passwords, client secrets) will be replaced with an empty value.")
            .WithName("GetAzureCredentials");
        
        group.MapGet("/azurecredentials/{azureCredentialId:guid}",
            async (ServiceDbContext dbContext, Guid azureCredentialId, CancellationToken cancellationToken) =>
            {
                var credential = await dbContext.AzureCredentials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AzureCredentialId == azureCredentialId, cancellationToken);
                if (credential is null)
                {
                    throw new NotFoundException<AzureCredential>(azureCredentialId);
                }
                return Results.Ok(credential);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<AzureCredential>()
            .WithSummary("Get Azure credential by id")
            .WithDescription("Get Azure credential by id. " +
                             "Sensitive data (password, client secret) will be replaced with an empty value.")
            .WithName("GetAzureCredential");
        
        group.MapGet("/credentials", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.Credentials
                    .AsNoTracking()
                    .OrderBy(x => x.Domain)
                    .ThenBy(x => x.Username)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<Credential[]>()
            .WithSummary("Get all on-premise/Windows credentials")
            .WithDescription("Get all on-premise/Windows credentials. Passwords will be replaced with an empty value.")
            .WithName("GetCredentials");
        
        group.MapGet("/credentials/{credentialId:guid}",
                async (ServiceDbContext dbContext, Guid credentialId, CancellationToken cancellationToken) =>
                {
                    var credential = await dbContext.Credentials
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.CredentialId == credentialId, cancellationToken);
                    if (credential is null)
                    {
                        throw new NotFoundException<Credential>(credentialId);
                    }
                    return Results.Ok(credential);
                })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Credential>()
            .WithSummary("Get on-premise/Windows credential by id")
            .WithDescription("Get on-premise/Windows credential by id. Password will be replaced with an empty value.")
            .WithName("GetCredential");
        
        group.MapGet("/databricksworkspaces", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.DatabricksWorkspaces
                    .AsNoTracking()
                    .OrderBy(x => x.WorkspaceName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<DatabricksWorkspace[]>()
            .WithSummary("Get all Databricks workspaces")
            .WithDescription("Get all Databricks workspaces. Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetDatabricksWorkspaces");
        
        group.MapGet("/databricksworkspaces/{workspaceId:guid}",
            async (ServiceDbContext dbContext, Guid workspaceId, CancellationToken cancellationToken) =>
            {
                var workspace = await dbContext.DatabricksWorkspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId, cancellationToken);
                if (workspace is null)
                {
                    throw new NotFoundException<DatabricksWorkspace>(workspaceId);
                }
                return Results.Ok(workspace);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DatabricksWorkspace>()
            .WithSummary("Get Databricks workspace by id")
            .WithDescription("Get Databricks workspace by id. Sensitive API token will be replaced with an empty value.")
            .WithName("GetDatabricksWorkspace");
        
        group.MapGet("/dbtaccounts", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.DbtAccounts
                    .AsNoTracking()
                    .OrderBy(x => x.DbtAccountName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<DbtAccount[]>()
            .WithSummary("Get all dbt accounts")
            .WithDescription("Get all dbt accounts. Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetDbtAccounts");
        
        group.MapGet("/dbtaccounts/{dbtAccountId:guid}",
            async (ServiceDbContext dbContext, Guid dbtAccountId, CancellationToken cancellationToken) =>
            {
                var account = await dbContext.DbtAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DbtAccountId == dbtAccountId, cancellationToken);
                if (account is null)
                {
                    throw new NotFoundException<DbtAccount>(dbtAccountId);
                }
                return Results.Ok(account);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DbtAccount>()
            .WithSummary("Get dbt account by id")
            .WithDescription("Get dbt account by id. Sensitive API token will be replaced with an empty value.")
            .WithName("GetDbtAccount");
        
        group.MapGet("/functionapps", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.FunctionApps
                    .AsNoTracking()
                    .OrderBy(x => x.FunctionAppId)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<FunctionApp[]>()
            .WithSummary("Get all Function Apps")
            .WithDescription("Get all Function Apps. Sensitive function keys will be replaced with an empty value.")
            .WithName("GetFunctionApps");
        
        group.MapGet("/functionapps/{functionAppId:guid}",
            async (ServiceDbContext dbContext, Guid functionAppId, CancellationToken cancellationToken) =>
            {
                var functionApp = await dbContext.FunctionApps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.FunctionAppId == functionAppId, cancellationToken);
                if (functionApp is null)
                {
                    throw new NotFoundException<FunctionApp>(functionAppId);
                }
                return Results.Ok(functionApp);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<FunctionApp>()
            .WithSummary("Get Function App by id")
            .WithDescription("Get Function App by id. Sensitive function key will be replaced with an empty value.")
            .WithName("GetFunctionApp");
        
        group.MapGet("/pipelineclients", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.PipelineClients
                    .AsNoTracking()
                    .OrderBy(c => c.PipelineClientId)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<PipelineClient[]>()
            .WithSummary("Get all pipeline clients")
            .WithDescription("Get all pipeline clients (Data Factory, Synapse Workspace).")
            .WithName("GetPipelineClients");
        
        group.MapGet("/pipelineclients/{pipelineClientId:guid}",
            async (ServiceDbContext dbContext, Guid pipelineClientId, CancellationToken cancellationToken) =>
            {
                var pipelineClient = await dbContext.PipelineClients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.PipelineClientId == pipelineClientId, cancellationToken);
                if (pipelineClient is null)
                {
                    throw new NotFoundException<PipelineClient>(pipelineClientId);
                }
                return Results.Ok(pipelineClient);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<PipelineClient>()
            .WithSummary("Get pipeline client by id")
            .WithDescription("Get pipeline client by id.")
            .WithName("GetPipelineClient");
        
        group.MapGet("/proxies", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
            await dbContext.Proxies
                .AsNoTracking()
                .OrderBy(x => x.ProxyName)
                .ToArrayAsync(cancellationToken))
            .Produces<Proxy[]>()
            .WithSummary("Get all proxies")
            .WithDescription("Get all proxies. Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetProxies");
        
        group.MapGet("/proxies/{proxyId:guid}",
            async (ServiceDbContext dbContext, Guid proxyId, CancellationToken cancellationToken) =>
            {
                var proxy = await dbContext.Proxies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ProxyId == proxyId, cancellationToken);
                if (proxy is null)
                {
                    throw new NotFoundException<Proxy>(proxyId);
                }
                return Results.Ok(proxy);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Proxy>()
            .WithSummary("Get proxy by id")
            .WithDescription("Get proxy by id. Sensitive API token will be replaced with an empty value.")
            .WithName("GetProxy");
        
        group.MapGet("/qlikcloudenvironments", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.QlikCloudEnvironments
                    .AsNoTracking()
                    .OrderBy(x => x.QlikCloudEnvironmentName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<QlikCloudEnvironment[]>()
            .WithSummary("Get all Qlik Cloud environments")
            .WithDescription("Get all Qlik Cloud environments. " +
                             "Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetQlikCloudEnvironments");
        
        group.MapGet("/qlikcloudenvironments/{qlikCloudEnvironmentId:guid}",
            async (ServiceDbContext dbContext, Guid qlikCloudEnvironmentId, CancellationToken cancellationToken) =>
            {
                var environment = await dbContext.QlikCloudEnvironments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.QlikCloudEnvironmentId == qlikCloudEnvironmentId, cancellationToken);
                if (environment is null)
                {
                    throw new NotFoundException<QlikCloudEnvironment>(qlikCloudEnvironmentId);
                }
                return Results.Ok(environment);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<QlikCloudEnvironment>()
            .WithSummary("Get Qlik Cloud environment by id")
            .WithDescription("Get Qlik Cloud environment by id. " +
                             "Sensitive API token will be replaced with an empty value.")
            .WithName("GetQlikCloudEnvironment");

        group.MapGet("/sqlconnections", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.SqlConnections
                    .AsNoTracking()
                    .OrderBy(c => c.ConnectionId)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<SqlConnectionBase[]>()
            .WithSummary("Get all SQL connections")
            .WithDescription("Get all SQL connections. Sensitive connection strings will be replaced with an empty value.")
            .WithName("GetSqlConnections");
        
        group.MapGet("/sqlconnections/{sqlConnectionId:guid}",
            async (ServiceDbContext dbContext, Guid sqlConnectionId, CancellationToken cancellationToken) =>
            {
                var connection = await dbContext.SqlConnections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ConnectionId == sqlConnectionId, cancellationToken);
                if (connection is null)
                {
                    throw new NotFoundException<SqlConnectionBase>(sqlConnectionId);
                }
                return Results.Ok(connection);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<SqlConnectionBase>()
            .WithSummary("Get SQL connection by id")
            .WithDescription("Get SQL connection by id. Sensitive connection string will be replaced with an empty value.")
            .WithName("GetSqlConnection");
    }
}