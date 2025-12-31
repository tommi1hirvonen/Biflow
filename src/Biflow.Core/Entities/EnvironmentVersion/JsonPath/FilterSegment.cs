using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class FilterSegment(IJsonPathFilter filter) : IJsonPathSegment
{
    public IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input)
    {
        foreach (var node in input)
        {
            if (node is not JsonArray arr) continue;
            
            foreach (var item in arr)
            {
                if (item != null && filter.Match(item))
                    yield return item;
            }
        }
    }
}
