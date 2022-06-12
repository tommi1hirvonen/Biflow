using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class PipelineStep : ParameterizedStep
{
    public PipelineStep() : base(StepType.Pipeline) { }

    [Required]
    public Guid? PipelineClientId { get; set; }

    [MaxLength(250)]
    [Display(Name = "Pipeline name")]
    [Required]
    public string? PipelineName { get; set; }

    public PipelineClient? PipelineClient { get; set; }
}
