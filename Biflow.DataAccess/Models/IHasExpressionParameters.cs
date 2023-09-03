namespace Biflow.DataAccess.Models;

public interface IHasExpressionParameters<TExpressionParameter, TJobParameter>
    where TExpressionParameter : IExpressionParameter<TJobParameter>
    where TJobParameter : ParameterBase
{
    public IList<TExpressionParameter> ExpressionParameters { get; }

    public IEnumerable<TJobParameter> JobParameters { get; }

    public void AddExpressionParameter(TJobParameter jobParameter);
}
