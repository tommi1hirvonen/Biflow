using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class FunctionStepParameter : StepParameterBase
{
    public FunctionStepParameter() : base(ParameterType.Function)
    {
    }

    internal FunctionStepParameter(FunctionStepParameter other, FunctionStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public FunctionStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
