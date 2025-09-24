using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class HttpStepParameter : StepParameterBase
{
    public HttpStepParameter() : base(ParameterType.Http)
    {
    }

    internal HttpStepParameter(HttpStepParameter other, HttpStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public HttpStep Step { get; init; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}