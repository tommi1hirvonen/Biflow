using Biflow.Core.Constants;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

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
        
        group.MapGet("/azurecredentials", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.AzureCredentials
                    .AsNoTracking()
                    .OrderBy(x => x.AzureCredentialName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all Azure credentials. " +
                             "Sensitive data (passwords, client secrets) will be replaced with an empty value.")
            .WithName("GetAzureCredentials");
        
        group.MapGet("/azurecredentials/{azureCredentialId:guid}",
                async (ServiceDbContext dbContext, Guid azureCredentialId, CancellationToken cancellationToken) =>
                {
                    var credential = await dbContext.AzureCredentials
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.AzureCredentialId == azureCredentialId, cancellationToken);
                    return credential is null ? Results.NotFound() : Results.Ok(credential);
                })
            .WithDescription("Get Azure credential by id. " +
                             "Sensitive data (password, client secret) will be replaced with an empty value.")
            .WithName("GetAzureCredential");

        group.MapGet("/sqlconnections", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.SqlConnections
                    .AsNoTracking()
                    .OrderBy(c => c.ConnectionId)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all SQL connections. Sensitive connection strings will be replaced with an empty value.")
            .WithName("GetSqlConnections");
        
        group.MapGet("/sqlconnections/{sqlConnectionId:guid}",
            async (ServiceDbContext dbContext, Guid sqlConnectionId, CancellationToken cancellationToken) =>
            {
                var connection = await dbContext.SqlConnections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ConnectionId == sqlConnectionId, cancellationToken);
                return connection is null ? Results.NotFound() : Results.Ok(connection);
            })
            .WithDescription("Get SQL connection by id. Sensitive connection string will be replaced with an empty value.")
            .WithName("GetSqlConnection");
        
        group.MapGet("/pipelineclients", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.PipelineClients
                    .AsNoTracking()
                    .OrderBy(c => c.PipelineClientId)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all pipeline clients (Data Factory, Synapse Workspace).")
            .WithName("GetPipelineClients");
        
        group.MapGet("/pipelineclients/{pipelineClientId:guid}",
                async (ServiceDbContext dbContext, Guid pipelineClientId, CancellationToken cancellationToken) =>
                {
                    var pipelineClient = await dbContext.PipelineClients
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.PipelineClientId == pipelineClientId, cancellationToken);
                    return pipelineClient is null ? Results.NotFound() : Results.Ok(pipelineClient);
                })
            .WithDescription("Get pipeline client by id.")
            .WithName("GetPipelineClient");
        
        group.MapGet("/functionapps", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.FunctionApps
                    .AsNoTracking()
                    .OrderBy(x => x.FunctionAppId)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all Function Apps. Sensitive function keys will be replaced with an empty value.")
            .WithName("GetFunctionApps");
        
        group.MapGet("/functionapps/{functionAppId:guid}",
                async (ServiceDbContext dbContext, Guid functionAppId, CancellationToken cancellationToken) =>
                {
                    var functionApp = await dbContext.FunctionApps
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.FunctionAppId == functionAppId, cancellationToken);
                    return functionApp is null ? Results.NotFound() : Results.Ok(functionApp);
                })
            .WithDescription("Get Function App by id. Sensitive function key will be replaced with an empty value.")
            .WithName("GetFunctionApp");
        
        group.MapGet("/databricksworkspaces", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.DatabricksWorkspaces
                    .AsNoTracking()
                    .OrderBy(x => x.WorkspaceName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all Databricks workspaces. Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetDatabricksWorkspaces");
        
        group.MapGet("/databricksworkspaces/{workspaceId:guid}",
                async (ServiceDbContext dbContext, Guid workspaceId, CancellationToken cancellationToken) =>
                {
                    var workspace = await dbContext.DatabricksWorkspaces
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId, cancellationToken);
                    return workspace is null ? Results.NotFound() : Results.Ok(workspace);
                })
            .WithDescription("Get Databricks workspace by id. Sensitive API token will be replaced with an empty value.")
            .WithName("GetDatabricksWorkspace");
        
        group.MapGet("/dbtaccounts", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.DbtAccounts
                    .AsNoTracking()
                    .OrderBy(x => x.DbtAccountId)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all dbt accounts. Sensitive API tokens will be replaced with an empty value.")
            .WithName("GetDbtAccounts");
        
        group.MapGet("/dbtaccounts/{dbtAccountId:guid}",
                async (ServiceDbContext dbContext, Guid dbtAccountId, CancellationToken cancellationToken) =>
                {
                    var account = await dbContext.DbtAccounts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.DbtAccountId == dbtAccountId, cancellationToken);
                    return account is null ? Results.NotFound() : Results.Ok(account);
                })
            .WithDescription("Get dbt account by id. Sensitive API token will be replaced with an empty value.")
            .WithName("GetDbtAccount");
        
        group.MapGet("/analysisservicesconnections", async (ServiceDbContext dbContext, CancellationToken cancellationToken) => 
                await dbContext.AnalysisServicesConnections
                    .AsNoTracking()
                    .OrderBy(x => x.ConnectionName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all SQL Server Analysis Services connections. " +
                             "Sensitive connection strings will be replaced with an empty value.")
            .WithName("GetAnalysisServicesConnections");
        
        group.MapGet("/analysisservicesconnections/{connectionId:guid}",
                async (ServiceDbContext dbContext, Guid connectionId, CancellationToken cancellationToken) =>
                {
                    var connection = await dbContext.AnalysisServicesConnections
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.ConnectionId == connectionId, cancellationToken);
                    return connection is null ? Results.NotFound() : Results.Ok(connection);
                })
            .WithDescription("Get SQL Server Analysis Services connection by id. " +
                             "Sensitive connection string will be replaced with an empty value.")
            .WithName("GetAnalysisServicesConnection");
    }
}