using CodingSeb.ExpressionEvaluator;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess.Models;

[Owned]
public class EvaluationExpression
{
    public string? Expression { get; set; }

    internal Task<object?> EvaluateAsync(IDictionary<string, object?>? parameters = null) =>
        EvaluateAsync<object?>(parameters);

    internal Task<bool> EvaluateBooleanAsync(IDictionary<string, object?>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            return Task.FromResult(true);
        }

        return EvaluateAsync<bool>(parameters);
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
        var result = evaluator.Evaluate<T>(Expression);
        return Task.Run(() => evaluator.Evaluate<T>(Expression));
    }

    public override string ToString() => Expression ?? "";

}
