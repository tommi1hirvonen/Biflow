using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("PipelineClient")]
[JsonDerivedType(typeof(DataFactory), nameof(PipelineClientType.DataFactory))]
[JsonDerivedType(typeof(SynapseWorkspace), nameof(PipelineClientType.Synapse))]
public abstract class PipelineClient(PipelineClientType type)
{
    [Key]
    [JsonInclude]
    public Guid PipelineClientId { get; private set; }

    [MaxLength(250)]
    [Required]
    public string PipelineClientName { get; set; } = "";

    public PipelineClientType PipelineClientType { get; } = type;

    [Required]
    [Display(Name = "App registration")]
    public Guid? AppRegistrationId { get; set; }

    [JsonIgnore]
    public AppRegistration AppRegistration { get; set; } = null!;

    [JsonIgnore]
    public IList<PipelineStep> Steps { get; set; } = null!;

    public abstract Task<string> StartPipelineRunAsync(string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

    public abstract Task<(string Status, string Message)> GetPipelineRunAsync(string runId, CancellationToken cancellationToken);

    public abstract Task CancelPipelineRunAsync(string runId);

    public abstract Task<PipelineFolder> GetPipelinesAsync();

    public abstract Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(string pipelineName);
}
