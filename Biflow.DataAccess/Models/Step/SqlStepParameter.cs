namespace Biflow.DataAccess.Models;

public class SqlStepParameter : StepParameterBase
{
    public SqlStepParameter() : base(ParameterType.Sql)
    {
    }

    public SqlStep Step { get; set; } = null!;

    public override Step BaseStep => Step; 
}
