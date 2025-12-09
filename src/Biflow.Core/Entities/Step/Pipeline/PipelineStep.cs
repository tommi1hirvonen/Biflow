using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

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

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid PipelineClientId { get; set; }

    [MaxLength(250)]
    [Required]
    public string PipelineName { get; set; } = "";

    [JsonIgnore]
    public PipelineClient? PipelineClient { get; set; }

    [ValidateComplexType]
    [JsonInclude]
    public IList<PipelineStepParameter> StepParameters { get; private set; } = new List<PipelineStepParameter>();
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Pipeline;

    public override PipelineStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new PipelineStepExecution(this, execution);
}
