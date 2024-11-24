using Biflow.Core.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.Core.Converters;

internal class DbtJobRunStatusConverter : JsonConverter<DbtJobRunStatus>
{
    public override DbtJobRunStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var status = reader.GetInt32();
        return status switch
        {
            1 => DbtJobRunStatus.Queued,
            2 => DbtJobRunStatus.Starting,
            3 => DbtJobRunStatus.Running,
            10 => DbtJobRunStatus.Success,
            20 => DbtJobRunStatus.Error,
            30 => DbtJobRunStatus.Cancelled,
            _ => throw new ArgumentException($"Unrecognized dbt job run status {status}")
        };
    }

    public override void Write(Utf8JsonWriter writer, DbtJobRunStatus value, JsonSerializerOptions options)
    {
        var status = value switch
        {
            DbtJobRunStatus.Queued => 1,
            DbtJobRunStatus.Starting => 2,
            DbtJobRunStatus.Running => 3,
            DbtJobRunStatus.Success => 10,
            DbtJobRunStatus.Error => 20,
            DbtJobRunStatus.Cancelled => 30,
            _ => throw new ArgumentException($"Unrecognized dbt job run status {value}")
        };
        writer.WriteNumberValue(status);
    }
}
