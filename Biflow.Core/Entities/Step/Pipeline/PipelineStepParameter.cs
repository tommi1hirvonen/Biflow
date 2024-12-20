using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class PipelineStepParameter : StepParameterBase
{
    public PipelineStepParameter() : base(ParameterType.Pipeline)
    {
    }

    internal PipelineStepParameter(PipelineStepParameter other, PipelineStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public PipelineStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
