using Biflow.Core.Converters;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonConverter(typeof(DbtJobRunStatusConverter))]
public enum DbtJobRunStatus
{
    Queued,
    Starting,
    Running,
    Success,
    Error,
    Cancelled
}
