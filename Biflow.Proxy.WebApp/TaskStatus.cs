using OneOf;
using OneOf.Types;

namespace Biflow.Proxy.WebApp;

[GenerateOneOf]
public partial class TaskStatus<T> : OneOfBase<Result<T>, Error<Exception>, Running, NotFound>;

public readonly struct Running;