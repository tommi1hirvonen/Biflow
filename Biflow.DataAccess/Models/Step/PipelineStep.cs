using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class PipelineStep : Step
{
    public PipelineStep() : base(StepType.Pipeline) { }

    [Required]
    public Guid? PipelineClientId { get; set; }

    [MaxLength(250)]
    [Display(Name = "Pipeline name")]
    [Required]
    public string? PipelineName { get; set; }

    public override bool SupportsParameterization => true;

    public PipelineClient? PipelineClient { get; set; }
}
