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
    }
}