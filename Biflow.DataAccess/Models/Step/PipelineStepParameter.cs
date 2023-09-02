namespace Biflow.DataAccess.Models;

public class PipelineStepParameter : StepParameterBase
{
    public PipelineStepParameter() : base(ParameterType.Pipeline)
    {
    }

    internal PipelineStepParameter(PipelineStepParameter other, PipelineStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    public PipelineStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
