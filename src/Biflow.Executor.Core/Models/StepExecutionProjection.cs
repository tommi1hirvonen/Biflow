using System.Text.Json.Serialization;

namespace Biflow.Executor.Core.Models;

[PublicAPI]
public record StepExecutionProjection(
    Guid StepId,
    string StepName,
    StepType StepType,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    StepExecutionStatus? ExecutionStatus,
    StepExecutionAttemptProjection[] ExecutionAttempts);