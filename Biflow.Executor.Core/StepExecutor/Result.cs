using OneOf;

namespace Biflow.Executor.Core.StepExecutor;

[GenerateOneOf]
internal partial class Result : OneOfBase<Success, Cancel, Failure>
{
    public static Result Success = new Success();

    public static Result Failure = new Failure();

    public static Result Cancel = new Cancel();
}

internal readonly struct Success;

internal readonly struct Failure;

internal readonly struct Cancel;