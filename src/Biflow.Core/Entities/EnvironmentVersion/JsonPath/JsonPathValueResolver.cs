using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal static class JsonPathValueResolver
{
    public static JsonNode? Resolve(JsonNode node, IReadOnlyList<string> path)
    {
        var current = node;

        foreach (var segment in path)
        {
            if (current is JsonObject obj && obj.TryGetPropertyValue(segment, out var value))
            {
                current = value;
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}
