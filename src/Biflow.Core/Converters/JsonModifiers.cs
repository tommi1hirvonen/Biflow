using System.Text.Json.Serialization.Metadata;
using Biflow.Core.Attributes;

namespace Biflow.Core.Converters;

public static class JsonModifiers
{
    public static void SensitiveModifier(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties.Where(p => p.PropertyType == typeof(string)))
        {
            var attributes = property.AttributeProvider
                ?.GetCustomAttributes(typeof(JsonSensitiveAttribute), true) ?? [];

            if (attributes.Length == 0)
            {
                continue;
            }

            var attribute = (JsonSensitiveAttribute)attributes[0];
            property.CustomConverter = new SensitiveStringConverter(attribute);
        }
    }
}