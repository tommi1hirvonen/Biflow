using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class PipelineStep : Step, IHasTimeout, IHasStepParameters<PipelineStepParameter>
{
    [JsonConstructor]
    public PipelineStep() : base(StepType.Pipeline) { }

    private PipelineStep(PipelineStep other, Job? targetJob) : base(other, targetJob) 
    {
        TimeoutMinutes = other.TimeoutMinutes;
        PipelineClientId = other.PipelineClientId;
        PipelineClient = other.PipelineClient;
        PipelineName = other.PipelineName;
        StepParameters = other.StepParameters
            .Select(p => new PipelineStepParameter(p, this, targetJob))
            .ToList();
    }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid PipelineClientId { get; set; }

    [MaxLength(250)]
    [Display(Name = "Pipeline name")]
    [Required]
    public string? PipelineName { get; set; }

    [JsonIgnore]
    public PipelineClient? PipelineClient { get; set; }

    [ValidateComplexType]
    public IList<PipelineStepParameter> StepParameters { get; set; } = null!;

    internal override PipelineStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new PipelineStepExecution(this, execution);
}
