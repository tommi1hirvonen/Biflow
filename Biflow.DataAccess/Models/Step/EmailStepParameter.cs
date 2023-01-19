namespace Biflow.DataAccess.Models;

public class EmailStepParameter : StepParameterBase
{
    public EmailStepParameter() : base(ParameterType.Email)
    {
    }

    public EmailStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
