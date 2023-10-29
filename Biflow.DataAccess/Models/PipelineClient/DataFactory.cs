using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
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

    public override async Task<IDictionary<string, IEnumerable<PipelineInfo>>> GetPipelinesAsync(ITokenService tokenService)
    {
        var client = GetArmClient(tokenService);
        var dataFactory = GetDataFactoryResource(client);

        var pipelines = new List<DataFactoryPipelineResource>();
        await foreach (var pipelineResource in dataFactory.GetDataFactoryPipelines().GetAllAsync())
        {
            pipelines.Add(pipelineResource);
        }

        static PipelineInfo infoFromData(DataFactoryPipelineData data)
        {
            var parameters = data.Parameters?.ToDictionary(p => p.Key, p => (p.Value.ParameterType.ToString(), p.Value.DefaultValue?.ToString()));
            return new(data.Name, parameters ?? []);
        };

        return pipelines
            .Select(p => (Folder: p.Data.FolderName ?? "/", Pipeline: p.Data))
            .GroupBy(p => p.Folder)
            .ToDictionary(g => g.Key, g => g.Select(p => infoFromData(p.Pipeline)).ToArray().AsEnumerable());
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
            return (p.Key, datatype, (object?)p.Value.DefaultValue?.ToString());
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
}
