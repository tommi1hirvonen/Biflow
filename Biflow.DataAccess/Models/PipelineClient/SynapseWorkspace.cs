using Azure.Analytics.Synapse.Artifacts;
using Azure.Analytics.Synapse.Artifacts.Models;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynapsePipelineClient = Azure.Analytics.Synapse.Artifacts.PipelineClient;

namespace Biflow.DataAccess.Models;

public class SynapseWorkspace() : PipelineClient(PipelineClientType.Synapse)
{
    [Required]
    [MaxLength(500)]
    [Unicode(false)]
    public string SynapseWorkspaceUrl { get; set; } = "";

    [NotMapped]
    private Uri SynapseEndpoint => new(SynapseWorkspaceUrl);

    private const string ResourceUrl = "https://dev.azuresynapse.net//.default";

    public override async Task CancelPipelineRunAsync(ITokenService tokenService, string runId)
    {
        var token = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var pipelineClient = new PipelineRunClient(SynapseEndpoint, token);
        await pipelineClient.CancelPipelineRunAsync(runId, isRecursive: true);
    }

    public override async Task<(string Status, string Message)> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken)
    {
        var token = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var pipelineClient = new PipelineRunClient(SynapseEndpoint, token);
        var run = await pipelineClient.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public override async Task<PipelineFolder> GetPipelinesAsync(ITokenService tokenService)
    {
        var token = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var pipelineClient = new SynapsePipelineClient(SynapseEndpoint, token);
        var pipelineResources = new List<PipelineResource>();
        await foreach (var pipeline in pipelineClient.GetPipelinesByWorkspaceAsync())
        {
            pipelineResources.Add(pipeline);
        }

        var pipelines = pipelineResources.Select(p =>
        {
            var folder = p.Folder?.Name;
            var parameters = p.Parameters.ToDictionary(p => p.Key, p => (p.Value.Type.ToString(), p.Value.DefaultValue?.ToString()));
            var pipeline = new PipelineInfo(p.Name, folder, parameters ?? []);
            return pipeline;
        });

        var folder = PipelineFolder.FromPipelines(pipelines);
        return folder;
    }

    public override async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(ITokenService tokenService, string pipelineName)
    {
        var token = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var client = new SynapsePipelineClient(SynapseEndpoint, token);
        var pipeline = await client.GetPipelineAsync(pipelineName);
        return pipeline.Value.Parameters.Select(param =>
        {
            var datatype = param.Value.Type.ToString() switch
            {
                "int" => ParameterValueType.Int32,
                "bool" => ParameterValueType.Boolean,
                "float" => ParameterValueType.Double,
                _ => ParameterValueType.String
            };
            return (param.Key, datatype, (object?)param.Value.DefaultValue);
        });
    }

    public override async Task<string> StartPipelineRunAsync(ITokenService tokenService, string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var token = new AzureTokenCredential(tokenService, AppRegistration, ResourceUrl);
        var pipelineClient = new SynapsePipelineClient(SynapseEndpoint, token);
        var response = await pipelineClient.CreatePipelineRunAsync(pipelineName, parameters: parameters, cancellationToken: cancellationToken);
        return response.Value.RunId;
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var token = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var pipelineClient = new SynapsePipelineClient(SynapseEndpoint, token);
        var pageable = pipelineClient.GetPipelinesByWorkspaceAsync();
        await foreach (var _ in pageable)
        {
        }
    }
}