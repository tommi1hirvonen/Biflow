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

    private const string ResourceUrl = "https://management.azure.com/";

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

    public override async Task<Dictionary<string, List<PipelineInfo>>> GetPipelinesAsync(ITokenService tokenService)
    {
        var client = await GetClientAsync(tokenService);
        var allPipelines = new List<IPage<PipelineResource>>();

        var pipelines = await client.Pipelines.ListByFactoryAsync(ResourceGroupName, ResourceName);
        allPipelines.Add(pipelines);
        var nextPage = pipelines.NextPageLink;

        while (nextPage is not null)
        {
            var pipelines_ = await client.Pipelines.ListByFactoryNextAsync(nextPage);
            allPipelines.Add(pipelines_);
            nextPage = pipelines_.NextPageLink;
        }

        static PipelineInfo infoFromResource(PipelineResource res)
        {
            var parameters = res.Parameters?.ToDictionary(p => p.Key, p => (p.Value.Type, p.Value?.DefaultValue));
            return new(res.Name, parameters ?? new Dictionary<string, (string Type, object? DefaultValue)>());
        };

        // Key = Folder
        // Value = List of pipelines in that folder
        return allPipelines
            .SelectMany(p => p.Select(p_ => (Folder: p_.Folder?.Name ?? "/", Pipeline: p_))) // Replace null folder (root) with forward slash.
            .GroupBy(p => p.Folder)
            .ToDictionary(p => p.Key, p => p.Select(p_ => infoFromResource(p_.Pipeline)).ToList());
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
        }) ?? Enumerable.Empty<(string, ParameterValueType, object?)>();
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var credential = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var context = new TokenRequestContext(new[] { ResourceUrl });
        var token = await credential.GetTokenAsync(context);

        var credentials = new TokenCredentials(token.Token);
        var client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
        var _ = await client.Factories.GetAsync(ResourceGroupName, ResourceName);
    }
}
