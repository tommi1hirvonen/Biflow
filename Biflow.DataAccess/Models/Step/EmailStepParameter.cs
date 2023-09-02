namespace Biflow.DataAccess.Models;

public class EmailStepParameter : StepParameterBase
{
    public EmailStepParameter() : base(ParameterType.Email)
    {
    }

    internal EmailStepParameter(EmailStepParameter other, EmailStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    public EmailStep Step { get; set; } = null!;

    public override Step BaseStep => Step;
}
