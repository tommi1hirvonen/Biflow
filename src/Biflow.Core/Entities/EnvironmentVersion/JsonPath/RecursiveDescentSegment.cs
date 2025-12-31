using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class RecursiveDescentSegment : IJsonPathSegment
{
    public IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input)
    {
        return input.SelectMany(Traverse);
    }

    private static IEnumerable<JsonNode?> Traverse(JsonNode? node)
    {
        if (node == null)
            yield break;

        yield return node;

        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj)
                    foreach (var d in Traverse(kv.Value))
                        yield return d;
                break;

            case JsonArray arr:
                foreach (var item in arr)
                    foreach (var d in Traverse(item))
                        yield return d;
                break;
        }
    }
}
