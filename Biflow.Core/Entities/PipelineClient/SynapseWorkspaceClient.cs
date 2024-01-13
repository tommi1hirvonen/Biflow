using Azure.Analytics.Synapse.Artifacts;
using Azure.Analytics.Synapse.Artifacts.Models;
using Biflow.Core.Interfaces;
using SynapsePipelineClient = Azure.Analytics.Synapse.Artifacts.PipelineClient;

namespace Biflow.Core.Entities;

internal class SynapseWorkspaceClient(SynapseWorkspace synapse, ITokenService tokenService) : IPipelineClient
{
    public async Task CancelPipelineRunAsync(string runId)
    {
        var token = new AzureTokenCredential(tokenService, synapse.AppRegistration, SynapseWorkspace.ResourceUrl);
        var pipelineClient = new PipelineRunClient(synapse.SynapseEndpoint, token);
        await pipelineClient.CancelPipelineRunAsync(runId, isRecursive: true);
    }

    public async Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        var token = new AzureTokenCredential(tokenService, synapse.AppRegistration, SynapseWorkspace.ResourceUrl);
        var pipelineClient = new PipelineRunClient(synapse.SynapseEndpoint, token);
        var run = await pipelineClient.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public async Task<PipelineFolder> GetPipelinesAsync()
    {
        var token = new AzureTokenCredential(tokenService, synapse.AppRegistration, SynapseWorkspace.ResourceUrl);
        var pipelineClient = new SynapsePipelineClient(synapse.SynapseEndpoint, token);
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

    public async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(string pipelineName)
    {
        var token = new AzureTokenCredential(tokenService, synapse.AppRegistration, SynapseWorkspace.ResourceUrl);
        var client = new SynapsePipelineClient(synapse.SynapseEndpoint, token);
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

    public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var token = new AzureTokenCredential(tokenService, synapse.AppRegistration, SynapseWorkspace.ResourceUrl);
        var pipelineClient = new SynapsePipelineClient(synapse.SynapseEndpoint, token);
        var response = await pipelineClient.CreatePipelineRunAsync(pipelineName, parameters: parameters, cancellationToken: cancellationToken);
        return response.Value.RunId;
    }
}
