using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManager.DataAccess.Models;

public class PipelineStepExecution : ParameterizedStepExecution
{
    public PipelineStepExecution(string stepName, string pipelineName) : base(stepName, StepType.Pipeline)
    {
        PipelineName = pipelineName;
    }

    [Display(Name = "Pipeline name")]
    public string PipelineName { get; set; }

    [Display(Name = "Data Factory id")]
    public Guid DataFactoryId { get; set; }

    public DataFactory DataFactory { get; set; } = null!;

    [Column("TimeoutMinutes")]
    public int TimeoutMinutes { get; set; }
}
