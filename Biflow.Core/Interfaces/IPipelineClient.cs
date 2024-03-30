using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IPipelineClient
{
    public Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

    public Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken);

    public Task CancelPipelineRunAsync(string runId);

    public Task<PipelineFolder> GetPipelinesAsync();

    public Task<IEnumerable<(string Name, ParameterValue Value)>> GetPipelineParametersAsync(string pipelineName);
}
