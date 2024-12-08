using Azure.Analytics.Synapse.Artifacts;
using Azure.Core;
using Biflow.Core.Interfaces;
using SynapsePipelineClient = Azure.Analytics.Synapse.Artifacts.PipelineClient;

namespace Biflow.Core.Entities;

internal class SynapseWorkspaceClient(SynapseWorkspace synapse, ITokenService tokenService) : IPipelineClient
{
    private readonly TokenCredential _tokenCredential =
        synapse.AzureCredential.GetTokenServiceCredential(tokenService);
    
    public async Task CancelPipelineRunAsync(string runId)
    {
        var pipelineClient = new PipelineRunClient(synapse.SynapseEndpoint, _tokenCredential);
        await pipelineClient.CancelPipelineRunAsync(runId, isRecursive: true);
    }

    public async Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        var pipelineClient = new PipelineRunClient(synapse.SynapseEndpoint, _tokenCredential);
        var run = await pipelineClient.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public async Task<PipelineFolder> GetPipelinesAsync()
    {
        var pipelineClient = new SynapsePipelineClient(synapse.SynapseEndpoint, _tokenCredential);
        var pipelineResources = await pipelineClient.GetPipelinesByWorkspaceAsync().ToListAsync();

        var pipelines = pipelineResources.Select(pipelineResource =>
        {
            var folder = pipelineResource.Folder?.Name;
            var parameters = pipelineResource.Parameters
                .ToDictionary(p => p.Key, p => (p.Value.Type.ToString(), p.Value.DefaultValue?.ToString()));
            var pipeline = new PipelineInfo(pipelineResource.Name, folder, parameters);
            return pipeline;
        });

        var folder = PipelineFolder.FromPipelines(pipelines);
        return folder;
    }

    public async Task<IEnumerable<(string Name, ParameterValue Value)>> GetPipelineParametersAsync(string pipelineName)
    {
        var client = new SynapsePipelineClient(synapse.SynapseEndpoint, _tokenCredential);
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
            _ = ParameterValue.TryCreate(datatype, param.Value.DefaultValue, out var value);
            return (param.Key, value);
        });
    }

    public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var pipelineClient = new SynapsePipelineClient(synapse.SynapseEndpoint, _tokenCredential);
        var response = await pipelineClient.CreatePipelineRunAsync(pipelineName, parameters: parameters, cancellationToken: cancellationToken);
        return response.Value.RunId;
    }
}
