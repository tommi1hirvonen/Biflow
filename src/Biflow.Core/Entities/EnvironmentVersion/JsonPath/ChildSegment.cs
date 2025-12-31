using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class ChildSegment(string propertyName) : IJsonPathSegment
{
    public IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input)
    {
        foreach (var node in input)
        {
            if (node is JsonObject obj && obj.TryGetPropertyValue(propertyName, out var value))
            {
                yield return value;
            }
        }
    }
}
