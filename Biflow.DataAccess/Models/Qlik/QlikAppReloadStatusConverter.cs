using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

internal class QlikAppReloadStatusConverter : JsonConverter<QlikAppReloadStatus>
{
    public override QlikAppReloadStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var status = reader.GetString();
        return status switch
        {
            "QUEUED" => QlikAppReloadStatus.Queued,
            "RELOADING" => QlikAppReloadStatus.Reloading,
            "CANCELING" => QlikAppReloadStatus.Canceling,
            "SUCCEEDED" => QlikAppReloadStatus.Succeeded,
            "FAILED" => QlikAppReloadStatus.Failed,
            "CANCELED" => QlikAppReloadStatus.Canceled,
            "EXCEEDED_LIMIT" => QlikAppReloadStatus.ExceededLimit,
            _ => throw new ApplicationException($"Unrecognized status {status}")
        };
    }

    public override void Write(Utf8JsonWriter writer, QlikAppReloadStatus value, JsonSerializerOptions options)
    {
        var status = value switch
        {
            QlikAppReloadStatus.Queued => "QUEUED",
            QlikAppReloadStatus.Reloading => "RELOADING",
            QlikAppReloadStatus.Canceling => "CANCELING",
            QlikAppReloadStatus.Succeeded => "SUCCEEDED",
            QlikAppReloadStatus.Failed => "FAILED",
            QlikAppReloadStatus.Canceled => "CANCELED",
            QlikAppReloadStatus.ExceededLimit => "EXCEEDED_LIMIT",
            _ => throw new ApplicationException($"Unrecognized status {value}")
        };
        writer.WriteStringValue(status);
    }
}