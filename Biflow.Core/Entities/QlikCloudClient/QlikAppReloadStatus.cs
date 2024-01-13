using System.Text.Json.Serialization;
using Biflow.Core.Converters;

namespace Biflow.Core.Entities;

[JsonConverter(typeof(QlikAppReloadStatusConverter))]
public enum QlikAppReloadStatus
{
    Queued,
    Reloading,
    Canceling,
    Succeeded,
    Failed,
    Canceled,
    ExceededLimit
}