using OneOf;
using OneOf.Types;

namespace Biflow.ExecutorProxy.WebApp;

[GenerateOneOf]
internal partial class TaskStatus<TResult, TStatus>
    : OneOfBase<Result<TResult>, Error<Exception>, Running<TStatus>, NotFound>;

internal readonly struct Running<T>(T value)
{
    public T Value { get; } = value;
}