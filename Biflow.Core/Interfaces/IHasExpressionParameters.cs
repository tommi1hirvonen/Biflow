using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasExpressionParameters<TExpressionParameter, TJobParameter>
    where TExpressionParameter : IExpressionParameter<TJobParameter>
    where TJobParameter : ParameterBase
{
    public IEnumerable<TExpressionParameter> ExpressionParameters { get; }

    public IEnumerable<TJobParameter> JobParameters { get; }

    public void AddExpressionParameter(TJobParameter jobParameter);

    public bool RemoveExpressionParameter(TExpressionParameter parameter);
}
