using OneOf;

namespace Biflow.Executor.Core.StepExecutor;

[GenerateOneOf]
internal partial class Result : OneOfBase<Success, Cancel, Failure> { }

internal readonly struct Success { }

internal readonly struct Failure { }

internal readonly struct Cancel { }