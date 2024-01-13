using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

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

    public abstract IPipelineClient CreatePipelineClient(ITokenService tokenService);
}
