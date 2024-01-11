using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

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
    public FunctionStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
