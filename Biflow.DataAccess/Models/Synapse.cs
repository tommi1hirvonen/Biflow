using Azure.Analytics.Synapse.Artifacts.Models;
using Azure.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class Synapse : PipelineClient
{
    public Synapse(string synapseWorkspaceUrl) : base(PipelineClientType.Synapse)
    {
        SynapseWorkspaceUrl = synapseWorkspaceUrl;
    }

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string SynapseWorkspaceUrl { get; set; }

    [NotMapped]
    private Uri SynapseEndpoint => new(SynapseWorkspaceUrl);

    public override async Task CancelPipelineRunAsync(ITokenService tokenService, string runId)
    {
        var token = new SynapseTokenCredential(tokenService, AppRegistration);
        var pipelineClient = new Azure.Analytics.Synapse.Artifacts.PipelineRunClient(SynapseEndpoint, token);
        await pipelineClient.CancelPipelineRunAsync(runId, isRecursive: true);
    }

    public override async Task<(string Status, string Message)> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken)
    {
        var token = new SynapseTokenCredential(tokenService, AppRegistration);
        var pipelineClient = new Azure.Analytics.Synapse.Artifacts.PipelineRunClient(SynapseEndpoint, token);
        var run = await pipelineClient.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public override async Task<Dictionary<string, List<PipelineInfo>>> GetPipelinesAsync(ITokenService tokenService)
    {
        var token = new SynapseTokenCredential(tokenService, AppRegistration);
        var pipelineClient = new Azure.Analytics.Synapse.Artifacts.PipelineClient(SynapseEndpoint, token);
        var list = new List<PipelineResource>();
        var pipelines = pipelineClient.GetPipelinesByWorkspaceAsync();
        await foreach (var pipeline in pipelines)
        {
            list.Add(pipeline);
        }

        static PipelineInfo infoFromResource(PipelineResource res)
        {
            var parameters = res.Parameters.ToDictionary(p => p.Key, p => (p.Value.Type.ToString(), p.Value.DefaultValue));
            return new(res.Name, parameters);
        };

        return list
            .Select(p => (Folder: p.Folder?.Name ?? "/", Pipeline: p))
            .GroupBy(p => p.Folder)
            .ToDictionary(p => p.Key, p => p.Select(p_ => infoFromResource(p_.Pipeline)).ToList());
    }

    public override async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(ITokenService tokenService, string pipelineName)
    {
        var token = new SynapseTokenCredential(tokenService, AppRegistration);
        var client = new Azure.Analytics.Synapse.Artifacts.PipelineClient(SynapseEndpoint, token);
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
        var token = new SynapseTokenCredential(tokenService, AppRegistration);
        var pipelineClient = new Azure.Analytics.Synapse.Artifacts.PipelineClient(SynapseEndpoint, token);
        var response = await pipelineClient.CreatePipelineRunAsync(pipelineName, parameters: parameters, cancellationToken: cancellationToken);
        return response.Value.RunId;
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var token = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var pipelineClient = new Azure.Analytics.Synapse.Artifacts.PipelineClient(SynapseEndpoint, token);
        var pageable = pipelineClient.GetPipelinesByWorkspaceAsync();
        await foreach (var _ in pageable)
        {
        }
    }
}