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
    }
}