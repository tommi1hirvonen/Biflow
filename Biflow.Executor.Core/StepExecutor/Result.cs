using OneOf;

namespace Biflow.Executor.Core.StepExecutor;

[GenerateOneOf]
internal partial class Result : OneOfBase<Success, Cancel, Failure> { }

internal readonly struct Success { }

internal readonly record struct Cancel(Exception? Exception = null) { }

internal readonly record struct Failure(Exception? Exception, string ErrorMessage)
{
    public Failure(string errorMessage) : this(null, errorMessage) { }
}