namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public class IntegrationsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.IntegrationsWrite]);
        
        var group = app.MapGroup("/integrations")
            .WithTags(Scopes.IntegrationsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapDelete("/analysisservicesconnections/{connectionId:guid}", async (Guid connectionId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteAnalysisServicesConnectionCommand(connectionId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete Analysis Services connection")
            .WithDescription("Delete SQL Server Analysis Services connection")
            .WithName("DeleteAnalysisServicesConnection");
        
        group.MapDelete("/azurecredentials/{azureCredentialId:guid}", async (Guid azureCredentialId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteAzureCredentialCommand(azureCredentialId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete Azure credential")
            .WithDescription("Delete Azure credential")
            .WithName("DeleteAzureCredential");
        
        group.MapDelete("/databricksworkspaces/{workspaceId:guid}", async (Guid workspaceId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteDatabricksWorkspaceCommand(workspaceId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete Databricks workspace")
            .WithDescription("Delete Databricks workspace")
            .WithName("DeleteDatabricksWorkspace");
        
        group.MapDelete("/dbtaccounts/{dbtAccountId:guid}", async (Guid dbtAccountId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteDbtAccountCommand(dbtAccountId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete dbt account")
            .WithDescription("Delete dbt account")
            .WithName("DeleteDbtAccount");
        
        group.MapDelete("/functionapps/{functionAppId:guid}", async (Guid functionAppId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteFunctionAppCommand(functionAppId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete Function App")
            .WithDescription("Delete Function App")
            .WithName("DeleteFunctionApp");
        
        group.MapDelete("/pipelineclients/{pipelineClientId:guid}", async (Guid pipelineClientId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeletePipelineClientCommand(pipelineClientId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete pipeline client")
            .WithDescription("Delete pipeline client")
            .WithName("DeletePipelineClient");
        
        group.MapDelete("/qlikcloudenvironments/{qlikCloudEnvironmentId:guid}", async (
                Guid qlikCloudEnvironmentId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteQlikCloudEnvironmentCommand(qlikCloudEnvironmentId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete Qlik Cloud environment")
            .WithDescription("Delete Qlik Cloud environment")
            .WithName("DeleteQlikCloudEnvironment");
        
        group.MapDelete("/sqlconnections/{connectionId:guid}", async (Guid connectionId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteSqlConnectionCommand(connectionId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete SQL connection")
            .WithDescription("Delete SQL connection (Snowflake or MS SQL)")
            .WithName("DeleteSqlConnection");
    }
}