using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(DataFactory), nameof(PipelineClientType.DataFactory))]
[JsonDerivedType(typeof(SynapseWorkspace), nameof(PipelineClientType.Synapse))]
public abstract class PipelineClient(PipelineClientType type)
{
    public Guid PipelineClientId { get; init; }

    [MaxLength(250)]
    [Required]
    public string PipelineClientName { get; set; } = "";

    public PipelineClientType PipelineClientType { get; } = type;

    [Required]
    public Guid? AzureCredentialId { get; set; }

    [JsonIgnore]
    public AzureCredential AzureCredential { get; init; } = null!;

    [Range(0, int.MaxValue)]
    public int MaxConcurrentPipelineSteps { get; set; }

    [JsonIgnore]
    public IEnumerable<PipelineStep> Steps { get; } = new List<PipelineStep>();

    public abstract IPipelineClient CreatePipelineClient(ITokenService tokenService);
}
