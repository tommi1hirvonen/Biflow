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

    public ExeStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
