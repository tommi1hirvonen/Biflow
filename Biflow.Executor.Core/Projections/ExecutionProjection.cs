using System.Text.Json.Serialization;

namespace Biflow.Executor.Core.Projections;

public record ExecutionProjection(
    Guid ExecutionId,
    Guid JobId,
    string JobName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ExecutionMode ExecutionMode,
    DateTimeOffset CreatedOn,
    DateTimeOffset? StartedOn,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ExecutionStatus ExecutionStatus,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    StepExecutionProjection[]? Steps);