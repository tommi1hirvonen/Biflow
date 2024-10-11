using Biflow.Core.Converters;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonConverter(typeof(QlikAutomationRunStatusConverter))]
public enum QlikAutomationRunStatus
{
    Failed,
    Finished,
    FinishedWithWarnings,
    MustStop,
    NotStarted,
    Running,
    Starting,
    Stopped
}
