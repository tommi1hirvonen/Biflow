using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class PropertyComparisonFilter(
    IEnumerable<string> propertyPath,
    Func<JsonNode?, bool> predicate) : IJsonPathFilter
{
    private readonly string[] _propertyPath = propertyPath.ToArray();

    public bool Match(JsonNode node)
    {
        var value = JsonPathValueResolver.Resolve(node, _propertyPath);
        return predicate(value);
    }
}

