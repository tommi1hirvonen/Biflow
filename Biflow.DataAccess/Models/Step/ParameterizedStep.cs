namespace Biflow.DataAccess.Models;

public abstract class ParameterizedStep : Step
{
    public ParameterizedStep(StepType stepType) : base(stepType)
    {
    }

    public IList<StepParameterBase> StepParameters { get; set; } = null!;
}
