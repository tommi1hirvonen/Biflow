using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class SqlStepParameter : StepParameterBase
{
    public SqlStepParameter() : base(ParameterType.Sql)
    {
    }

    internal SqlStepParameter(SqlStepParameter other, SqlStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public SqlStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step; 
}
