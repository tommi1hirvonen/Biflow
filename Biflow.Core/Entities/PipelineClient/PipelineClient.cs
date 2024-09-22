using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(DataFactory), nameof(PipelineClientType.DataFactory))]
[JsonDerivedType(typeof(SynapseWorkspace), nameof(PipelineClientType.Synapse))]
public abstract class PipelineClient(PipelineClientType type)
{
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

    [Range(0, int.MaxValue)]
    public int MaxConcurrentPipelineSteps { get; set; } = 0;

    [JsonIgnore]
    public IEnumerable<PipelineStep> Steps { get; } = new List<PipelineStep>();

    public abstract IPipelineClient CreatePipelineClient(ITokenService tokenService);
}
