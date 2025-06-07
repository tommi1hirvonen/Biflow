using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DatabricksStepParameter : StepParameterBase
{
    public DatabricksStepParameter() : base(ParameterType.DatabricksNotebook)
    {
    }

    internal DatabricksStepParameter(DatabricksStepParameter other, DatabricksStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public DatabricksStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
