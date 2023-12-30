using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public abstract class DynamicParameter : ParameterBase, IAsyncEvaluable
{
    public bool UseExpression { get; set; }

    public EvaluationExpression Expression { get; set; } = new();

    [JsonIgnore]
    public override string DisplayValue => UseExpression switch
    {
        true => $"{Expression.Expression} (expression)",
        false => base.DisplayValue
    };

    [JsonIgnore]
    public override string DisplayValueType => UseExpression ? "Dynamic" : base.DisplayValueType;

    public abstract Task<object?> EvaluateAsync();
}