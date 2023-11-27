using Biflow.Scheduler.Core;
using OneOf;

namespace Biflow.Ui.Core;

[GenerateOneOf]
public partial class SchedulerStatusResponse : OneOfBase<Success, AuthorizationError, SchedulerError, UndefinedError> { }

public record Success(IEnumerable<JobStatus> Jobs);

public readonly record struct AuthorizationError();

public readonly record struct SchedulerError();

public readonly record struct UndefinedError();