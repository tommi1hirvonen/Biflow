namespace Biflow.DataAccess.Models;

public interface IAsyncEvaluable
{
    public Task<object?> EvaluateAsync();
}
