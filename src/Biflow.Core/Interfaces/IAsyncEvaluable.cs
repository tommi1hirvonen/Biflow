namespace Biflow.Core.Interfaces;

public interface IAsyncEvaluable
{
    public Task<object?> EvaluateAsync();
}
