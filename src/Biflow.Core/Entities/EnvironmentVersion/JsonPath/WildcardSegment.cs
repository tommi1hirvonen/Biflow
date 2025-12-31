using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class WildcardSegment : IJsonPathSegment
{
    public IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input)
    {
        foreach (var node in input)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var kv in obj)
                        yield return kv.Value;
                    break;

                case JsonArray arr:
                    foreach (var item in arr)
                        yield return item;
                    break;
            }
        }
    }
}
