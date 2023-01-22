using CodingSeb.ExpressionEvaluator;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess.Models;

[Owned]
public class EvaluationExpression
{
    public string? Expression { get; set; }

    public async Task<object?> EvaluateAsync(IDictionary<string, object?>? parameters = null) =>
        await EvaluateAsync<object?>(parameters);

    public async Task<bool> EvaluateBooleanAsync(IDictionary<string, object?>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            return true;
        }

        return await EvaluateAsync<bool>(parameters);
    }

    private async Task<T> EvaluateAsync<T>(IDictionary<string, object?>? parameters = null)
    {
        parameters ??= new Dictionary<string, object?>();
        var evaluator = new ExpressionEvaluator
        {
            OptionScriptEvaluateFunctionActive = false,
            OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate = false
        };
        evaluator.Namespaces.Remove("System.IO");
        evaluator.Variables = parameters;
        return await Task.Run(() => evaluator.Evaluate<T>(Expression));
    }

    public override string ToString() => Expression ?? "";

}
