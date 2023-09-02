namespace Biflow.DataAccess.Models;

public class SqlStepParameter : StepParameterBase
{
    public SqlStepParameter() : base(ParameterType.Sql)
    {
    }

    internal SqlStepParameter(SqlStepParameter other, SqlStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    public SqlStep Step { get; set; } = null!;

    public override Step BaseStep => Step; 
}
