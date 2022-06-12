using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PipelineStepExecution : ParameterizedStepExecution
{
    public PipelineStepExecution(string stepName, string pipelineName) : base(stepName, StepType.Pipeline)
    {
        PipelineName = pipelineName;
    }

    [Display(Name = "Pipeline name")]
    public string PipelineName { get; set; }

    [Display(Name = "Pipeline client id")]
    public Guid PipelineClientId { get; set; }

    public PipelineClient PipelineClient { get; set; } = null!;

    [Column("TimeoutMinutes")]
    public int TimeoutMinutes { get; set; }
}
