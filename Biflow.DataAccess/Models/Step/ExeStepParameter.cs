using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class ExeStepParameter : StepParameterBase
{
    public ExeStepParameter() : base(ParameterType.Exe)
    {
    }

    internal ExeStepParameter(ExeStepParameter other, ExeStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public ExeStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
