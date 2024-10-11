using Biflow.Core.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.Core.Converters;

internal class QlikAutomationRunStatusConverter : JsonConverter<QlikAutomationRunStatus>
{
    public override QlikAutomationRunStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var status = reader.GetString();
        return status switch
        {
            "failed" => QlikAutomationRunStatus.Failed,
            "finished" => QlikAutomationRunStatus.Finished,
            "finished with warnings" => QlikAutomationRunStatus.FinishedWithWarnings,
            "must stop" => QlikAutomationRunStatus.MustStop,
            "not started" => QlikAutomationRunStatus.NotStarted,
            "running" =>QlikAutomationRunStatus.Running,
            "starting" => QlikAutomationRunStatus.Starting,
            "stopped" => QlikAutomationRunStatus.Stopped,
            _ => throw new ApplicationException($"Unrecognized status {status}")
        };
    }

    public override void Write(Utf8JsonWriter writer, QlikAutomationRunStatus value, JsonSerializerOptions options)
    {
        var status = value switch
        {
            QlikAutomationRunStatus.Failed => "failed",
            QlikAutomationRunStatus.Finished => "finished",
            QlikAutomationRunStatus.FinishedWithWarnings => "finished with warnings",
            QlikAutomationRunStatus.MustStop => "must stop",
            QlikAutomationRunStatus.NotStarted => "not started",
            QlikAutomationRunStatus.Running => "running",
            QlikAutomationRunStatus.Starting => "starting",
            QlikAutomationRunStatus.Stopped => "stopped",
            _ => throw new ApplicationException($"Unrecognized status {value}")
        };
        writer.WriteStringValue(status);
    }
}
