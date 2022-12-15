using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("PipelineClient")]
public abstract class PipelineClient
{
    public PipelineClient(PipelineClientType type)
    {
        PipelineClientType = type;
    }

    [Key]
    public Guid PipelineClientId { get; set; }

    public string? PipelineClientName { get; set; }

    public PipelineClientType PipelineClientType { get; }

    [Required]
    [Display(Name = "App registration")]
    public Guid? AppRegistrationId { get; set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    public IList<PipelineStep> Steps { get; set; } = null!;

    public abstract Task<string> StartPipelineRunAsync(ITokenService tokenService, string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

    public abstract Task<(string Status, string Message)> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken);

    public abstract Task CancelPipelineRunAsync(ITokenService tokenService, string runId);

    public abstract Task<Dictionary<string, List<PipelineInfo>>> GetPipelinesAsync(ITokenService tokenService);

    public abstract Task<IEnumerable<(string Name, ParameterValueType Type, object? Default)>> GetPipelineParametersAsync(ITokenService tokenService, string pipelineName);
}
