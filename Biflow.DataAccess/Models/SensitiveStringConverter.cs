using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class SensitiveStringConverter(JsonSensitiveAttribute attribute) : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString();

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        value = (value, attribute.WhenContains) switch
        {
            (null, _) => null,
            (_, null or { Length: 0 }) => attribute.Replacement,
            _ when value.Contains(attribute.WhenContains, attribute.StringComparison) => attribute.Replacement,
            _ => value
        };
        writer.WriteStringValue(value);
    }
}