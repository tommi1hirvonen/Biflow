namespace Biflow.DataAccess.Models;

public abstract class DynamicParameter : ParameterBase, IAsyncEvaluable
{
    public bool UseExpression { get; set; }

    public EvaluationExpression Expression { get; set; } = new();

    public override string DisplayValue => UseExpression switch
    {
        true => Expression.Expression ?? "",
        false => base.DisplayValue
    };

    public override string DisplayValueType => UseExpression ? "Dynamic" : base.DisplayValueType;

    public abstract Task<object?> EvaluateAsync();
}