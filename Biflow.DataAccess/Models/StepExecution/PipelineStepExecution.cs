using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PipelineStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<PipelineStepExecutionParameter>
{
    public PipelineStepExecution(string stepName, string pipelineName) : base(stepName, StepType.Pipeline)
    {
        PipelineName = pipelineName;
    }

    [Display(Name = "Pipeline name")]
    public string PipelineName { get; private set; }

    [Display(Name = "Pipeline client id")]
    public Guid PipelineClientId { get; private set; }

    public PipelineClient PipelineClient { get; set; } = null!;

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    public IList<PipelineStepExecutionParameter> StepExecutionParameters { get; set; } = null!;
}
