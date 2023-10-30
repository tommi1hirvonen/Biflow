using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class DataFactory : PipelineClient
{
    public DataFactory() : base(PipelineClientType.DataFactory) { }

    [Column("SubscriptionId")]
    [Required]
    [Display(Name = "Subscription id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string? SubscriptionId { get; set; }

    [Column("ResourceGroupName")]
    [Required]
    [Display(Name = "Resource group name")]
    public string? ResourceGroupName { get; set; }

    [Column("ResourceName")]
    [Required]
    [Display(Name = "Resource name")]
    public string? ResourceName { get; set; }

    private const string ResourceUrl = "https://management.azure.com//.default";

    private async Task<DataFactoryManagementClient> GetClientAsync(ITokenService tokenService)
    {
        var (accessToken, _) = await tokenService.GetTokenAsync(AppRegistration, ResourceUrl);
        var credentials = new TokenCredentials(accessToken);
        return new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
    }

    public override async Task<string> StartPipelineRunAsync(ITokenService tokenService, string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        var createRunResponse = await client.Pipelines.CreateRunAsync(ResourceGroupName, ResourceName, pipelineName,
            parameters: parameters, cancellationToken: cancellationToken);
        return createRunResponse.RunId;
    }

    public override async Task<(string Status, string Message)> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        var run = await client.PipelineRuns.GetAsync(ResourceGroupName, ResourceName, runId, cancellationToken);
        return (run.Status, run.Message);
    }

    public override async Task CancelPipelineRunAsync(ITokenService tokenService, string runId)
    {
        var client = await GetClientAsync(tokenService);
        await client.PipelineRuns.CancelAsync(ResourceGroupName, ResourceName, runId, isRecursive: true);
    }

    public override async Task<PipelineFolder> GetPipelinesAsync(ITokenService tokenService)
    {
        var client = await GetClientAsync(tokenService);
        var allPipelines = new List<IPage<PipelineResource>>();

        var pipelineResources = await client.Pipelines.ListByFactoryAsync(ResourceGroupName, ResourceName);
        allPipelines.Add(pipelineResources);
        var nextPage = pipelineResources.NextPageLink;

        while (nextPage is not null)
        {
            var pipelines_ = await client.Pipelines.ListByFactoryNextAsync(nextPage);
            allPipelines.Add(pipelines_);
            nextPage = pipelines_.NextPageLink;
        }

        var pipelines = pipelineResources.Select(p =>
        {
            var folder = p.Folder?.Name;
            var parameters = p.Parameters?.ToDictionary(p => p.Key, p => (p.Value.Type, p.Value?.DefaultValue?.ToString()));
            var pipeline = new PipelineInfo(p.Name, folder, parameters ?? []);
            return pipeline;
        });

        var folder = PipelineFolder.FromPipelines(pipelines);
        return folder;
    }

    public override async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(ITokenService tokenService, string pipelineName)
    {
        var client = await GetClientAsync(tokenService);
        var pipeline = await client.Pipelines.GetAsync(ResourceGroupName, ResourceName, pipelineName);
        return pipeline.Parameters?.Select(param =>
        {
            var datatype = param.Value.Type switch
            {
                "int" => ParameterValueType.Int32,
                "bool" => ParameterValueType.Boolean,
                "float" => ParameterValueType.Double,
                _ => ParameterValueType.String
            };
            return (param.Key, datatype, (object?)param.Value.DefaultValue);
        }) ?? [];
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var credential = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var context = new TokenRequestContext([ResourceUrl]);
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
        var _ = await client.Factories.GetAsync(ResourceGroupName, ResourceName);
    }

    #region The following is an implementation with the new Azure Resource Manager SDK (Azure.ResourceManager.DataFactory). Take into use when affecting bugs have been fixed https://github.com/Azure/azure-sdk-for-net/issues/39187
    /*
    public override async Task<string> StartPipelineRunAsync(ITokenService tokenService, string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var client = GetArmClient(tokenService);
        var resource = GetPipelineResource(client, pipelineName);
        var parameterSpecification = parameters.ToDictionary(key => key.Key, value => new BinaryData(value.Value));
        var result = await resource.CreateRunAsync(parameterValueSpecification: parameterSpecification, cancellationToken: cancellationToken);
        return result.Value.RunId.ToString();
    }

    public override async Task<(string Status, string Message)> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken)
    {
        var client = GetArmClient(tokenService);
        var resource = GetDataFactoryResource(client);
        var run = await resource.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public override async Task CancelPipelineRunAsync(ITokenService tokenService, string runId)
    {
        var client = GetArmClient(tokenService);
        var resource = GetDataFactoryResource(client);
        await resource.CancelPipelineRunAsync(runId, true);
    }

    public override async Task<PipelineFolder> GetPipelinesAsync(ITokenService tokenService)
    {
        var client = GetArmClient(tokenService);
        var dataFactory = GetDataFactoryResource(client);

        var pipelineResources = new List<DataFactoryPipelineResource>();
        await foreach (var pipelineResource in dataFactory.GetDataFactoryPipelines().GetAllAsync())
        {
            pipelineResources.Add(pipelineResource);
        }

        var pipelines = pipelineResources.Select(p =>
        {
            var folder = (string?)p.Data.FolderName;
            var parameters = p.Data.Parameters?.ToDictionary(p => p.Key, p => (p.Value.ParameterType.ToString(), p.Value.DefaultValue?.ToString()));
            var pipeline = new PipelineInfo(p.Data.Name, folder, parameters ?? []);
            return pipeline;
        });

        var folder = PipelineFolder.FromPipelines(pipelines);
        return folder;
    }

    public override async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(ITokenService tokenService, string pipelineName)
    {
        var client = GetArmClient(tokenService);
        var dataFactory = GetDataFactoryResource(client);
        var pipeline = await dataFactory.GetDataFactoryPipelineAsync(pipelineName);
        return pipeline.Value.Data.Parameters.Select(p =>
        {
            var datatype = ParameterValueType.String;
            if (p.Value.ParameterType == EntityParameterType.Int)
                datatype = ParameterValueType.Int32;
            else if (p.Value.ParameterType == EntityParameterType.Float)
                datatype = ParameterValueType.Double;
            else if (p.Value.ParameterType == EntityParameterType.Bool)
                datatype = ParameterValueType.Boolean;

            object? defaultValue = datatype switch
            {
                ParameterValueType.String => p.Value.DefaultValue?.ToObjectFromJson<string>(),
                ParameterValueType.Int32 => p.Value.DefaultValue?.ToObjectFromJson<int>(),
                ParameterValueType.Double => p.Value.DefaultValue?.ToObjectFromJson<double>(),
                ParameterValueType.Boolean => p.Value.DefaultValue?.ToObjectFromJson<bool>(),
                _ => p.Value.DefaultValue?.ToString()
            };

            return (p.Key, datatype, defaultValue);
        }).ToArray();
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var credential = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var client = new ArmClient(credential);
        var dataFactory = GetDataFactoryResource(client);
        var _ = await dataFactory.GetAsync();
    }

    private ArmClient GetArmClient(ITokenService tokenService)
    {
        var credentials = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var client = new ArmClient(credentials);
        return client;
    }

    private DataFactoryResource GetDataFactoryResource(ArmClient client)
    {
        var identifier = DataFactoryResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, ResourceName);
        var resource = client.GetDataFactoryResource(identifier);
        return resource;
    }

    private DataFactoryPipelineResource GetPipelineResource(ArmClient client, string pipelineName)
    {
        var identifier = DataFactoryPipelineResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, ResourceName, pipelineName);
        var resource = client.GetDataFactoryPipelineResource(identifier);
        return resource;
    }
    */
    #endregion 
}
