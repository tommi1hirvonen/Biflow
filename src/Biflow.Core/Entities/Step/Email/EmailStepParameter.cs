using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class EmailStepParameter : StepParameterBase
{
    public EmailStepParameter() : base(ParameterType.Email)
    {
    }

    internal EmailStepParameter(EmailStepParameter other, EmailStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public EmailStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
