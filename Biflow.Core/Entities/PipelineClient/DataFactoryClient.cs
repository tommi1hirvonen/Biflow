using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using Biflow.Core.Interfaces;
using System.Text.RegularExpressions;

namespace Biflow.Core.Entities;

internal class DataFactoryClient : IPipelineClient
{
    private readonly DataFactory _dataFactory;
    private readonly ArmClient _armClient;
    private readonly DataFactoryResource _dataFactoryResource;

    public DataFactoryClient(DataFactory dataFactory, ITokenService tokenService)
    {
        _dataFactory = dataFactory;
        var credentials = new AzureTokenCredential(tokenService, dataFactory.AppRegistration, DataFactory.ResourceUrl);
        _armClient = new ArmClient(credentials);
        var identifier = DataFactoryResource.CreateResourceIdentifier(dataFactory.SubscriptionId, dataFactory.ResourceGroupName, dataFactory.ResourceName);
        _dataFactoryResource = _armClient.GetDataFactoryResource(identifier);
    }

    public async Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var resource = GetPipelineResource(pipelineName);
        var parameterSpecification = parameters.ToDictionary(key => key.Key, value => new BinaryData(value.Value));
        var result = await resource.CreateRunAsync(parameterValueSpecification: parameterSpecification, cancellationToken: cancellationToken);
        return result.Value.RunId.ToString();
    }

    public async Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        var run = await _dataFactoryResource.GetPipelineRunAsync(runId, cancellationToken);
        return (run.Value.Status, run.Value.Message);
    }

    public Task CancelPipelineRunAsync(string runId) => _dataFactoryResource.CancelPipelineRunAsync(runId, true);

    public async Task<PipelineFolder> GetPipelinesAsync()
    {
        var pipelineResources = new List<DataFactoryPipelineResource>();
        await foreach (var pipelineResource in _dataFactoryResource.GetDataFactoryPipelines().GetAllAsync())
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

    public async Task<IEnumerable<(string Name, ParameterValue Value)>> GetPipelineParametersAsync(string pipelineName)
    {
        var pipeline = await _dataFactoryResource.GetDataFactoryPipelineAsync(pipelineName);
        return pipeline.Value.Data.Parameters.Select(p =>
        {
            var defaultValue = p.Value.DefaultValue.ToString();
            if (p.Value.ParameterType == EntityParameterType.Int && int.TryParse(defaultValue, out var i))
            {
                _ = ParameterValue.TryCreate(ParameterValueType.Int32, i, out var param);
                return (p.Key, param);
            }
            else if (p.Value.ParameterType == EntityParameterType.Bool && bool.TryParse(defaultValue, out var b))
            {
                _ = ParameterValue.TryCreate(ParameterValueType.Boolean, b, out var param);
                return (p.Key, param);
            }
            else if (p.Value.ParameterType == EntityParameterType.Float && double.TryParse(defaultValue, out var d))
            {
                _ = ParameterValue.TryCreate(ParameterValueType.Double, d, out var param);
                return (p.Key, param);
            }
            else
            {
                defaultValue = Regex.Unescape(defaultValue);
                defaultValue = defaultValue is ['"', .. var x, '"'] ? x : defaultValue;
                _ = ParameterValue.TryCreate(ParameterValueType.String, defaultValue, out var param);
                return (p.Key, param);
            }
        }).ToArray();
    }

    private DataFactoryPipelineResource GetPipelineResource(string pipelineName)
    {
        var identifier = DataFactoryPipelineResource.CreateResourceIdentifier(
            _dataFactory.SubscriptionId,
            _dataFactory.ResourceGroupName,
            _dataFactory.ResourceName,
            pipelineName);
        var resource = _armClient.GetDataFactoryPipelineResource(identifier);
        return resource;
    }
}