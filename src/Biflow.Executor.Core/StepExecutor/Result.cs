using OneOf;

namespace Biflow.Executor.Core.StepExecutor;

[GenerateOneOf]
internal partial class Result : OneOfBase<Success, Cancel, Failure>
{
    public static readonly Result Success = new Success();

    public static readonly Result Failure = new Failure();

    public static readonly Result Cancel = new Cancel();
}

internal class Success;

internal class Failure;

internal class Cancel;