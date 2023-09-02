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

    public FunctionStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
