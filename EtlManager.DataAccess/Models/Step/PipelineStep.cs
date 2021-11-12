using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class PipelineStep : ParameterizedStep
{
    public PipelineStep() : base(StepType.Pipeline) { }

    [Required]
    public Guid? DataFactoryId { get; set; }

    [MaxLength(250)]
    [Display(Name = "Pipeline name")]
    [Required]
    public string? PipelineName { get; set; }

    public DataFactory? DataFactory { get; set; }
}
