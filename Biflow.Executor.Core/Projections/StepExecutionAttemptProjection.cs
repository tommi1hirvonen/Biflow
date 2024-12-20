using System.Text.Json.Serialization;

namespace Biflow.Executor.Core.Projections;

[PublicAPI]
public record StepExecutionAttemptProjection(
    int RetryAttemptIndex,
    DateTimeOffset? StartedOn,
    DateTimeOffset? EndedOn,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    StepExecutionStatus ExecutionStatus);