using Biflow.Ui.Api.Mediator.Commands;
using Biflow.Ui.Api.Models.Integrations;

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

        #region Analysis Services connections
        
        group.MapPost("/analysisservicesconnections",
            async (CreateAnalysisServicesConnection dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateAnalysisServicesConnectionCommand(
                    dto.ConnectionName, dto.ConnectionString, dto.CredentialId);
                var connection = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetAnalysisServicesConnection",
                    new { connectionId = connection.ConnectionId });
                return Results.Created(url, connection);
            })
            .ProducesValidationProblem()
            .Produces<AnalysisServicesConnection>()
            .WithSummary("Create Analysis Services connection")
            .WithDescription("Create a new SQL Server Analysis Services connection")
            .WithName("CreateAnalysisServicesConnection");
        
        group.MapPut("/analysisservicesconnections/{connectionId:guid}",
            async (Guid connectionId, UpdateAnalysisServicesConnection dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateAnalysisServicesConnectionCommand(
                    connectionId, dto.ConnectionName, dto.ConnectionString, dto.CredentialId);
                var connection = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(connection);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<AnalysisServicesConnection>()
            .WithSummary("Update Analysis Services connection")
            .WithDescription("Update an existing SQL Server Analysis Services connection. " +
                             "Pass null as ConnectionString in the request body JSON model to retain the previous " +
                             "connection string value.")
            .WithName("UpdateAnalysisServicesConnection");
        
        group.MapDelete("/analysisservicesconnections/{connectionId:guid}",
            async (Guid connectionId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion

        #region Azure credentials
        
        group.MapPost("/azurecredentials/organizationalaccount",
            async (CreateOrganizationalAccountAzureCredential dto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateOrganizationalAccountAzureCredentialCommand(
                    dto.AzureCredentialName,
                    dto.TenantId,
                    dto.ClientId,
                    dto.Username,
                    dto.Password);
                var credential = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetAzureCredential",
                    new { azureCredentialId = credential.AzureCredentialId });
                return Results.Created(url, credential);
            })
            .ProducesValidationProblem()
            .Produces<OrganizationalAccountAzureCredential>()
            .WithSummary("Create organizational account Azure credential")
            .WithDescription("Create a new organizational account Azure credential")
            .WithName("CreateOrganizationalAccountAzureCredential");
        
        group.MapPut("/azurecredentials/organizationalaccount/{azureCredentialId:guid}",
            async (Guid azureCredentialId, UpdateOrganizationalAccountAzureCredential dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateOrganizationalAccountAzureCredentialCommand(
                    azureCredentialId,
                    dto.AzureCredentialName,
                    dto.TenantId,
                    dto.ClientId,
                    dto.Username,
                    dto.Password);
                var credential = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(credential);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<OrganizationalAccountAzureCredential>()
            .WithSummary("Update organizational account Azure credential")
            .WithDescription("Update an existing organizational account Azure credential. " +
                             "Pass null as Password in the request body JSON model to retain the previous " +
                             "password value.")
            .WithName("UpdateOrganizationalAccountAzureCredential");
        
        group.MapPost("/azurecredentials/serviceprincipal",
            async (CreateServicePrincipalAzureCredential dto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateServicePrincipalAzureCredentialCommand(
                    dto.AzureCredentialName,
                    dto.TenantId,
                    dto.ClientId,
                    dto.ClientSecret);
                var credential = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetAzureCredential",
                    new { azureCredentialId = credential.AzureCredentialId });
                return Results.Created(url, credential);
            })
            .ProducesValidationProblem()
            .Produces<ServicePrincipalAzureCredential>()
            .WithSummary("Create service principal Azure credential")
            .WithDescription("Create a new service principal Azure credential")
            .WithName("CreateServicePrincipalAzureCredential");
        
        group.MapPut("/azurecredentials/serviceprincipal/{azureCredentialId:guid}",
            async (Guid azureCredentialId, UpdateServicePrincipalAzureCredential dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateServicePrincipalAzureCredentialCommand(
                    azureCredentialId,
                    dto.AzureCredentialName,
                    dto.TenantId,
                    dto.ClientId,
                    dto.ClientSecret);
                var credential = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(credential);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<ServicePrincipalAzureCredential>()
            .WithSummary("Update service principal Azure credential")
            .WithDescription("Update an existing service principal Azure credential. " +
                             "Pass null as ClientSecret in the request body JSON model to retain the previous " +
                             "client secret value.")
            .WithName("UpdateServicePrincipalAzureCredential");
        
        group.MapPost("/azurecredentials/managedidentity",
            async (ManagedIdentityAzureCredentialDto dto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateManagedIdentityAzureCredentialCommand(
                    dto.AzureCredentialName,
                    dto.ClientId);
                var credential = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetAzureCredential",
                    new { azureCredentialId = credential.AzureCredentialId });
                return Results.Created(url, credential);
            })
            .ProducesValidationProblem()
            .Produces<ManagedIdentityAzureCredential>()
            .WithSummary("Create managed identity Azure credential")
            .WithDescription("Create a new managed identity Azure credential")
            .WithName("CreateManagedIdentityAzureCredential");
        
        group.MapPut("/azurecredentials/managedidentity/{azureCredentialId:guid}",
            async (Guid azureCredentialId, ManagedIdentityAzureCredentialDto dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateManagedIdentityAzureCredentialCommand(
                    azureCredentialId,
                    dto.AzureCredentialName,
                    dto.ClientId);
                var credential = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(credential);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<ManagedIdentityAzureCredential>()
            .WithSummary("Update managed identity Azure credential")
            .WithDescription("Update an existing managed identity Azure credential")
            .WithName("UpdateManagedIdentityAzureCredential");
        
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
        
        #endregion
        
        #region Credentials (on-premise)
        
        group.MapPost("/credentials",
            async (CredentialDto dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateCredentialCommand(
                    dto.Domain, dto.Username, dto.Password);
                var credential = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetCredential",
                    new { credentialId = credential.CredentialId });
                return Results.Created(url, credential);
            })
            .ProducesValidationProblem()
            .Produces<Credential>()
            .WithSummary("Create on-premise/Windows credential")
            .WithDescription("Create a new on-premise/Windows credential")
            .WithName("CreateCredential");
        
        group.MapPut("/credentials/{credentialId:guid}",
            async (Guid credentialId, CredentialDto dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateCredentialCommand(
                    credentialId, dto.Domain, dto.Username, dto.Password);
                var credential = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(credential);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Credential>()
            .WithSummary("Update on-premise/Windows credential")
            .WithDescription("Update an existing on-premise/Windows credential. " +
                             "Pass null as Password in the request body JSON model to retain the previous " +
                             "password value.")
            .WithName("UpdateCredential");
        
        group.MapDelete("/credentials/{credentialId:guid}",
            async (Guid credentialId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteCredentialCommand(credentialId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete on-premise/Windows credential")
            .WithDescription("Delete on-premise/Windows credential")
            .WithName("DeleteCredential");
        
        #endregion
        
        #region Databricks workspaces
        
        group.MapPost("/databricksworkspaces",
            async (CreateDatabricksWorkspace dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateDatabricksWorkspaceCommand(
                    dto.WorkspaceName, dto.WorkspaceUrl, dto.ApiToken);
                var workspace = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetDatabricksWorkspace",
                    new { workspaceId = workspace.WorkspaceId });
                return Results.Created(url, workspace);
            })
            .ProducesValidationProblem()
            .Produces<DatabricksWorkspace>()
            .WithSummary("Create Databricks workspace")
            .WithDescription("Create a new Databricks workspace")
            .WithName("CreateDatabricksWorkspace");
        
        group.MapPut("/databricksworkspaces/{workspaceId:guid}",
            async (Guid workspaceId, UpdateDatabricksWorkspace dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateDatabricksWorkspaceCommand(
                    workspaceId, dto.WorkspaceName, dto.WorkspaceUrl, dto.ApiToken);
                var workspace = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(workspace);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DatabricksWorkspace>()
            .WithSummary("Update Databricks workspace")
            .WithDescription("Update an existing Databricks workspace. " +
                             "Pass null as ApiToken in the request body JSON model to retain the previous " +
                             "API token value.")
            .WithName("UpdateDatabricksWorkspace");
        
        group.MapDelete("/databricksworkspaces/{workspaceId:guid}",
            async (Guid workspaceId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
        
        #region dbt accounts
        
        group.MapPost("/dbtaccounts",
            async (CreateDbtAccount dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateDbtAccountCommand(
                    DbtAccountName: dto.DbtAccountName,
                    ApiBaseUrl: dto.ApiBaseUrl,
                    AccountId: dto.AccountId,
                    ApiToken: dto.ApiToken);
                var account = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetDbtAccount",
                    new { dbtAccountId = account.DbtAccountId });
                return Results.Created(url, account);
            })
            .ProducesValidationProblem()
            .Produces<DbtAccount>()
            .WithSummary("Create dbt account")
            .WithDescription("Create a new dbt account")
            .WithName("CreateDbtAccount");
        
        group.MapPut("/dbtaccounts/{dbtAccountId:guid}",
            async (Guid dbtAccountId, UpdateDbtAccount dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateDbtAccountCommand(
                    DbtAccountId: dbtAccountId,
                    DbtAccountName: dto.DbtAccountName,
                    ApiBaseUrl: dto.ApiBaseUrl,
                    AccountId: dto.AccountId,
                    ApiToken: dto.ApiToken);
                var account = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(account);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DbtAccount>()
            .WithSummary("Update dbt account")
            .WithDescription("Update an existing dbt account. " +
                             "Pass null as ApiToken in the request body JSON model to retain the previous " +
                             "API token value.")
            .WithName("UpdateDbtAccount");
        
        group.MapDelete("/dbtaccounts/{dbtAccountId:guid}",
            async (Guid dbtAccountId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
        
        #region Function Apps
        
        group.MapPost("/functionapps",
            async (FunctionAppDto dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateFunctionAppCommand(
                    FunctionAppName: dto.FunctionAppName, 
                    SubscriptionId: dto.SubscriptionId, 
                    ResourceGroupName: dto.ResourceGroupName, 
                    ResourceName: dto.ResourceName, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    MaxConcurrentFunctionSteps: dto.MaxConcurrentFunctionSteps, 
                    FunctionAppKey: dto.FunctionAppKey);
                var functionApp = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetFunctionApp",
                    new { functionAppId = functionApp.FunctionAppId });
                return Results.Created(url, functionApp);
            })
            .ProducesValidationProblem()
            .Produces<FunctionApp>()
            .WithSummary("Create Function App")
            .WithDescription("Create a new Function App")
            .WithName("CreateFunctionApp");
        
        group.MapPut("/functionapps/{functionAppId:guid}",
            async (Guid functionAppId, FunctionAppDto dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateFunctionAppCommand(
                    FunctionAppId: functionAppId,
                    FunctionAppName: dto.FunctionAppName, 
                    SubscriptionId: dto.SubscriptionId, 
                    ResourceGroupName: dto.ResourceGroupName, 
                    ResourceName: dto.ResourceName, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    MaxConcurrentFunctionSteps: dto.MaxConcurrentFunctionSteps, 
                    FunctionAppKey: dto.FunctionAppKey);
                var functionApp = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(functionApp);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<FunctionApp>()
            .WithSummary("Update Function App")
            .WithDescription("Update an existing Function App. " +
                             "Pass null as FunctionAppKey in the request body JSON model to retain the previous " +
                             "app key value.")
            .WithName("UpdateFunctionApp");
        
        group.MapDelete("/functionapps/{functionAppId:guid}",
            async (Guid functionAppId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
        
        #region Pipeline clients
        
        group.MapPost("/pipelineclients/datafactory",
            async (DataFactoryDto dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateDataFactoryCommand(
                    PipelineClientName: dto.PipelineClientName, 
                    MaxConcurrentPipelineSteps: dto.MaxConcurrentPipelineSteps, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    SubscriptionId: dto.SubscriptionId, 
                    ResourceGroupName: dto.ResourceGroupName, 
                    ResourceName: dto.ResourceName);
                var dataFactory = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetPipelineClient",
                    new { pipelineClientId = dataFactory.PipelineClientId });
                return Results.Created(url, dataFactory);
            })
            .ProducesValidationProblem()
            .Produces<DataFactory>()
            .WithSummary("Create Data Factory")
            .WithDescription("Create a new Data Factory")
            .WithName("CreateDataFactory");
        
        group.MapPut("/pipelineclients/datafactory/{pipelineClientId:guid}",
            async (Guid pipelineClientId, DataFactoryDto dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateDataFactoryCommand(
                    PipelineClientId: pipelineClientId, 
                    PipelineClientName: dto.PipelineClientName, 
                    MaxConcurrentPipelineSteps: dto.MaxConcurrentPipelineSteps, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    SubscriptionId: dto.SubscriptionId, 
                    ResourceGroupName: dto.ResourceGroupName, 
                    ResourceName: dto.ResourceName);
                var dataFactory = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(dataFactory);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<DataFactory>()
            .WithSummary("Update Data Factory")
            .WithDescription("Update an existing Data Factory")
            .WithName("UpdateDataFactory");
        
        group.MapPost("/pipelineclients/synapseworkspace",
            async (SynapseWorkspaceDto dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateSynapseWorkspaceCommand(
                    PipelineClientName: dto.PipelineClientName, 
                    MaxConcurrentPipelineSteps: dto.MaxConcurrentPipelineSteps, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    SynapseWorkspaceUrl: dto.SynapseWorkspaceUrl);
                var workspace = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetPipelineClient",
                    new { pipelineClientId = workspace.PipelineClientId });
                return Results.Created(url, workspace);
            })
            .ProducesValidationProblem()
            .Produces<SynapseWorkspace>()
            .WithSummary("Create Synapse workspace")
            .WithDescription("Create a new Synapse workspace")
            .WithName("CreateSynapseWorkspace");
        
        group.MapPut("/pipelineclients/synapseworkspace/{pipelineClientId:guid}",
            async (Guid pipelineClientId, SynapseWorkspaceDto dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateSynapseWorkspaceCommand(
                    PipelineClientId: pipelineClientId, 
                    PipelineClientName: dto.PipelineClientName, 
                    MaxConcurrentPipelineSteps: dto.MaxConcurrentPipelineSteps, 
                    AzureCredentialId: dto.AzureCredentialId, 
                    SynapseWorkspaceUrl: dto.SynapseWorkspaceUrl);
                var connection = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(connection);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<SynapseWorkspace>()
            .WithSummary("Update Synapse workspace")
            .WithDescription("Update an existing Synapse workspace")
            .WithName("UpdateSynapseWorkspace");
        
        group.MapDelete("/pipelineclients/{pipelineClientId:guid}",
            async (Guid pipelineClientId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
        
        #region Qlik Cloud environments
        
        group.MapPost("/qlikcloudenvironments",
            async (CreateQlikCloudEnvironment dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateQlikCloudEnvironmentCommand(
                    dto.QlikCloudEnvironmentName, dto.EnvironmentUrl, dto.ApiToken);
                var environment = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetQlikCloudEnvironment",
                    new { qlikCloudEnvironmentId = environment.QlikCloudEnvironmentId });
                return Results.Created(url, environment);
            })
            .ProducesValidationProblem()
            .Produces<QlikCloudEnvironment>()
            .WithSummary("Create Qlik Cloud environment")
            .WithDescription("Create a new Qlik Cloud environment")
            .WithName("CreateQlikCloudEnvironment");
        
        group.MapPut("/qlikcloudenvironments/{qlikCloudEnvironmentId:guid}",
            async (Guid qlikCloudEnvironmentId, UpdateQlikCloudEnvironment dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateQlikCloudEnvironmentCommand(
                    qlikCloudEnvironmentId, dto.QlikCloudEnvironmentName, dto.EnvironmentUrl, dto.ApiToken);
                var environment = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(environment);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<QlikCloudEnvironment>()
            .WithSummary("Update Qlik Cloud environment")
            .WithDescription("Update an existing Qlik Cloud environment. " +
                             "Pass null as ApiToken in the request body JSON model to retain the previous " +
                             "API token value.")
            .WithName("UpdateQlikCloudEnvironment");
        
        group.MapDelete("/qlikcloudenvironments/{qlikCloudEnvironmentId:guid}",
            async (Guid qlikCloudEnvironmentId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
        
        #region SQL connections
        
        group.MapPost("/sqlconnections/mssql",
            async (CreateMsSqlConnection dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateMsSqlConnectionCommand(
                    ConnectionName: dto.ConnectionName, 
                    MaxConcurrentSqlSteps: dto.MaxConcurrentSqlSteps, 
                    MaxConcurrentPackageSteps: dto.MaxConcurrentPackageSteps, 
                    ExecutePackagesAsLogin: dto.ExecutePackagesAsLogin, 
                    CredentialId: dto.CredentialId, 
                    ScdDefaultTargetSchema: dto.ScdDefaultTargetSchema, 
                    ScdDefaultTargetTableSuffix: dto.ScdDefaultTargetTableSuffix, 
                    ScdDefaultStagingSchema: dto.ScdDefaultStagingSchema, 
                    ScdDefaultStagingTableSuffix: dto.ScdDefaultStagingTableSuffix, 
                    ConnectionString: dto.ConnectionString);
                var connection = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSqlConnection",
                    new { sqlConnectionId = connection.ConnectionId });
                return Results.Created(url, connection);
            })
            .ProducesValidationProblem()
            .Produces<MsSqlConnection>()
            .WithSummary("Create MS SQL connection")
            .WithDescription("Create a new MS SQL connection")
            .WithName("CreateMsSqlConnection");
        
        group.MapPut("/sqlconnections/mssql/{sqlConnectionId:guid}",
            async (Guid sqlConnectionId, UpdateMsSqlConnection dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateMsSqlConnectionCommand(
                    ConnectionId: sqlConnectionId,
                    ConnectionName: dto.ConnectionName, 
                    MaxConcurrentSqlSteps: dto.MaxConcurrentSqlSteps, 
                    MaxConcurrentPackageSteps: dto.MaxConcurrentPackageSteps, 
                    ExecutePackagesAsLogin: dto.ExecutePackagesAsLogin, 
                    CredentialId: dto.CredentialId, 
                    ScdDefaultTargetSchema: dto.ScdDefaultTargetSchema, 
                    ScdDefaultTargetTableSuffix: dto.ScdDefaultTargetTableSuffix, 
                    ScdDefaultStagingSchema: dto.ScdDefaultStagingSchema, 
                    ScdDefaultStagingTableSuffix: dto.ScdDefaultStagingTableSuffix, 
                    ConnectionString: dto.ConnectionString);
                var connection = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(connection);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MsSqlConnection>()
            .WithSummary("Update MS SQL connection")
            .WithDescription("Update an existing MS SQL connection")
            .WithName("UpdateMsSqlConnection");
        
        group.MapPost("/sqlconnections/snowflake",
            async (CreateSnowflakeConnection dto, IMediator mediator, LinkGenerator linker, HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateSnowflakeConnectionCommand(
                    ConnectionName: dto.ConnectionName, 
                    MaxConcurrentSqlSteps: dto.MaxConcurrentSqlSteps, 
                    ScdDefaultTargetSchema: dto.ScdDefaultTargetSchema, 
                    ScdDefaultTargetTableSuffix: dto.ScdDefaultTargetTableSuffix, 
                    ScdDefaultStagingSchema: dto.ScdDefaultStagingSchema, 
                    ScdDefaultStagingTableSuffix: dto.ScdDefaultStagingTableSuffix, 
                    ConnectionString: dto.ConnectionString);
                var connection = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetSqlConnection",
                    new { sqlConnectionId = connection.ConnectionId });
                return Results.Created(url, connection);
            })
            .ProducesValidationProblem()
            .Produces<SnowflakeConnection>()
            .WithSummary("Create Snowflake connection")
            .WithDescription("Create a new Snowflake connection")
            .WithName("CreateSnowflakeConnection");
        
        group.MapPut("/sqlconnections/snowflake/{sqlConnectionId:guid}",
            async (Guid sqlConnectionId, UpdateSnowflakeConnection dto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateSnowflakeConnectionCommand(
                    ConnectionId: sqlConnectionId,
                    ConnectionName: dto.ConnectionName, 
                    MaxConcurrentSqlSteps: dto.MaxConcurrentSqlSteps, 
                    ScdDefaultTargetSchema: dto.ScdDefaultTargetSchema, 
                    ScdDefaultTargetTableSuffix: dto.ScdDefaultTargetTableSuffix, 
                    ScdDefaultStagingSchema: dto.ScdDefaultStagingSchema, 
                    ScdDefaultStagingTableSuffix: dto.ScdDefaultStagingTableSuffix, 
                    ConnectionString: dto.ConnectionString);
                var connection = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(connection);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<SnowflakeConnection>()
            .WithSummary("Update Snowflake connection")
            .WithDescription("Update an existing Snowflake connection")
            .WithName("UpdateSnowflakeConnection");
        
        group.MapDelete("/sqlconnections/{connectionId:guid}",
            async (Guid connectionId, IMediator mediator, CancellationToken cancellationToken) =>
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
        
        #endregion
    }
}