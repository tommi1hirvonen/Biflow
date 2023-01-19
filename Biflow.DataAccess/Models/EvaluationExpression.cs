using CodingSeb.ExpressionEvaluator;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess.Models;

[Owned]
public class EvaluationExpression
{
    public string? Expression { get; set; }

    public object? Evaluate(IDictionary<string, object>? parameters = null)
    {
        parameters ??= new Dictionary<string, object>();
        var evaluator = GetEvaluator(parameters);
        // Evaluate the expression/statement with a separate Task to allow the calling thread to continue.
        return evaluator.Evaluate(Expression);
    }

    public async Task<bool> EvaluateBooleanAsync(IDictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(Expression))
        {
            return true;
        }

        parameters ??= new Dictionary<string, object>();
        var evaluator = GetEvaluator(parameters);
        // Evaluate the expression/statement with a separate Task to allow the calling thread to continue.
        return await Task.Run(() => evaluator.Evaluate<bool>(Expression));
    }

    private static ExpressionEvaluator GetEvaluator(IDictionary<string, object> parameters)
    {
        var evaluator = new ExpressionEvaluator
        {
            OptionScriptEvaluateFunctionActive = false,
            OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate = false
        };
        evaluator.Namespaces.Remove("System.IO");
        evaluator.Variables = parameters;
        return evaluator;
    }
}
