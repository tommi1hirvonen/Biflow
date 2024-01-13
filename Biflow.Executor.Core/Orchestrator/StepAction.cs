using Biflow.Core.Entities;
using OneOf;

namespace Biflow.Executor.Core.Orchestrator;

[GenerateOneOf]
internal partial class StepAction : OneOfBase<Execute, Cancel, Fail>;

internal readonly struct Execute;

internal readonly struct Cancel;

internal readonly record struct Fail(StepExecutionStatus WithStatus, string? ErrorMessage)
{
    public Fail(StepExecutionStatus withStatus) : this(withStatus, null) { }
}