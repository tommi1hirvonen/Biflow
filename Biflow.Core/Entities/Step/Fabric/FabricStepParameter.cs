using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class FabricStepParameter : StepParameterBase
{
    public FabricStepParameter() : base(ParameterType.Fabric)
    {
        
    }

    internal FabricStepParameter(FabricStepParameter other, FabricStep step, Job? job)
        : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public FabricStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}