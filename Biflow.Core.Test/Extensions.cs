using System.Reflection;
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

    public static void SetPrivatePropertyValue<T>(this object obj, string propertyName, T value, Type? declaringType = null)
    {
        var type = declaringType ?? obj.GetType();
        if (type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is PropertyInfo prop)
        {
            prop.SetValue(
                obj: obj,
                value: value,
                invokeAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                index: null,
                culture: null);
            return;
        }

        throw new ArgumentOutOfRangeException(
                nameof(propertyName),
                $"Property {propertyName} was not found in type {obj.GetType().FullName}");
    }
}
