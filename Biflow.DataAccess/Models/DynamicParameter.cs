namespace Biflow.DataAccess.Models;

public abstract class DynamicParameter : ParameterBase, IAsyncEvaluable
{
    public bool UseExpression { get; set; }

    public EvaluationExpression Expression { get; set; } = new();

    public string DisplayValue => (UseExpression, ParameterValue) switch
    {
        (true, _) => Expression.Expression ?? "",
        (false, null) => "null",
        _ => ParameterValue.ToString() ?? ""
    };

    public string DisplaySummary => (UseExpression, DisplayValue) switch
    {
        (true, { Length: <45 }) => $"{ParameterName} (Dynamic = {DisplayValue})",
        (true, _) => $"{ParameterName} (Dynamic = {DisplayValue[..40]}...)",
        (false, { Length: <45 }) => $"{ParameterName} ({ParameterValueType} = {DisplayValue})",
        (false, _) => $"{ParameterName} ({ParameterValueType} = {DisplayValue[..40]}...)"
    };

    public abstract Task<object?> EvaluateAsync();
}