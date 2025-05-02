using OneOf;
using OneOf.Types;

namespace Biflow.Proxy.WebApp;

[GenerateOneOf]
public partial class TaskStatus<TResult, TStatus>
    : OneOfBase<Result<TResult>, Error<Exception>, Running<TStatus>, NotFound>;

public readonly struct Running<T>(T value)
{
    public T Value { get; } = value;
}