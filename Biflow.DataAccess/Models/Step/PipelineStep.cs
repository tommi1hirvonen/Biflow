using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PipelineStep : Step, IHasTimeout, IHasStepParameters<PipelineStepParameter>
{
    public PipelineStep() : base(StepType.Pipeline) { }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid? PipelineClientId { get; set; }

    [MaxLength(250)]
    [Display(Name = "Pipeline name")]
    [Required]
    public string? PipelineName { get; set; }

    public PipelineClient? PipelineClient { get; set; }

    [ValidateComplexType]
    public IList<PipelineStepParameter> StepParameters { get; set; } = null!;

    public override StepExecution ToStepExecution(Execution execution) => new PipelineStepExecution(this, execution);
}
