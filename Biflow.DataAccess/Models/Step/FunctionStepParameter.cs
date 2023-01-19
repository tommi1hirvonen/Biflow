namespace Biflow.DataAccess.Models;

public class FunctionStepParameter : StepParameterBase
{
    public FunctionStepParameter() : base(ParameterType.Function)
    {
    }

    public FunctionStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
