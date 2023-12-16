using System.Text.Json.Serialization.Metadata;

namespace Biflow.DataAccess.Models;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsonSensitiveAttribute : Attribute
{
    public string? WhenContains { get; set; }

    public string Replacement { get; set; } = "";

    public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    public static void SensitiveModifier(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties.Where(p => p.PropertyType == typeof(string)))
        {
            var attributes = property.AttributeProvider
                ?.GetCustomAttributes(typeof(JsonSensitiveAttribute), true)
                ?? [];

            if (attributes.Length == 0)
            {
                continue;
            }

            var attribute = (JsonSensitiveAttribute)attributes[0];
            property.CustomConverter = new SensitiveStringConverter(attribute);
        }
    }
}