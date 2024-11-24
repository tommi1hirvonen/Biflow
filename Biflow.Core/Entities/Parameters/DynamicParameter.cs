using Biflow.Core.Interfaces;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public abstract class DynamicParameter : ParameterBase, IAsyncEvaluable
{
    public virtual bool UseExpression
    {
        get;
        set
        {
            field = value;
            if (field)
            {
                ParameterValue = new();
            }
        }
    }

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