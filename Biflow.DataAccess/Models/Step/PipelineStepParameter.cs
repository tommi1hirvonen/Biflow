namespace Biflow.DataAccess.Models;

public class PipelineStepParameter : StepParameterBase
{
    public PipelineStepParameter() : base(ParameterType.Pipeline)
    {
    }

    public PipelineStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
