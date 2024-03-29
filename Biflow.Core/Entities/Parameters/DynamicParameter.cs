using Biflow.Core.Interfaces;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public abstract class DynamicParameter : ParameterBase, IAsyncEvaluable
{
    public virtual bool UseExpression
    {
        get => _useExpression;
        set
        {
            _useExpression = value;
            if (_useExpression)
            {
                ParameterValue = new();
            }
        }
    }

    private bool _useExpression;

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