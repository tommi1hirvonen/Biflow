using System.Text.Json;

namespace Biflow.Core.Test;

internal static class Extensions
{
    public static T JsonRoundtrip<T>(this T obj, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(obj, options);
        var deserialized = JsonSerializer.Deserialize<T>(json, options);
        ArgumentNullException.ThrowIfNull(deserialized);
        return deserialized;
    }
}
