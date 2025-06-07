using CodingSeb.ExpressionEvaluator;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

[ComplexType]
public class EvaluationExpression
{
    public string? Expression { get; set; }

    internal Task<object?> EvaluateAsync(IDictionary<string, object?>? parameters = null) =>
        EvaluateAsync<object?>(parameters);

    internal Task<bool> EvaluateBooleanAsync(IDictionary<string, object?>? parameters = null)
    {
        return string.IsNullOrWhiteSpace(Expression)
            ? Task.FromResult(true)
            : EvaluateAsync<bool>(parameters);
    }

    private Task<T> EvaluateAsync<T>(IDictionary<string, object?>? parameters = null)
    {
        parameters ??= new Dictionary<string, object?>();
        var evaluator = new ExpressionEvaluator
        {
            OptionScriptEvaluateFunctionActive = false,
            OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate = false
        };
        evaluator.Namespaces.Remove("System.IO");
        evaluator.Variables = parameters;
        return Task.Run(() => evaluator.Evaluate<T>(Expression));
    }

    public override string ToString() => Expression ?? "";

}
