using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

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