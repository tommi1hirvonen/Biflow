using Biflow.Core.Interfaces;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Rest.Azure;
using Microsoft.Rest;
using Microsoft.Azure.Management.DataFactory.Models;

namespace Biflow.Core.Entities;

internal class DataFactoryClient(DataFactory dataFactory, ITokenService tokenService) : IPipelineClient
{
    private async Task<DataFactoryManagementClient> GetClientAsync()
    {
        var (accessToken, _) = await tokenService.GetTokenAsync(dataFactory.AppRegistration, DataFactory.ResourceUrl);
        var credentials = new TokenCredentials(accessToken);
        return new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };
    }

    public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var createRunResponse = await client.Pipelines.CreateRunAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, pipelineName,
            parameters: parameters, cancellationToken: cancellationToken);
        return createRunResponse.RunId;
    }

    public async Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync();
        var run = await client.PipelineRuns.GetAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, runId, cancellationToken);
        return (run.Status, run.Message);
    }

    public async Task CancelPipelineRunAsync(string runId)
    {
        var client = await GetClientAsync();
        await client.PipelineRuns.CancelAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, runId, isRecursive: true);
    }

    public async Task<PipelineFolder> GetPipelinesAsync()
    {
        var client = await GetClientAsync();
        var allPipelines = new List<IPage<PipelineResource>>();

        var pipelineResources = await client.Pipelines.ListByFactoryAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName);
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

    public async Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(string pipelineName)
    {
        var client = await GetClientAsync();
        var pipeline = await client.Pipelines.GetAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, pipelineName);
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
}
