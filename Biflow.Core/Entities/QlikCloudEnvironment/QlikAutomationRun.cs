using Biflow.Core.Converters;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public record QlikAutomationRun(
    string Id,
    QlikAutomationRunStatus Status,
    [property:JsonConverter(typeof(RawTextConverter))] string? Error);