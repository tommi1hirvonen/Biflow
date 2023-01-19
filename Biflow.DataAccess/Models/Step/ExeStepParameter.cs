namespace Biflow.DataAccess.Models;

public class ExeStepParameter : StepParameterBase
{
    public ExeStepParameter() : base(ParameterType.Exe)
    {
    }

    public ExeStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
