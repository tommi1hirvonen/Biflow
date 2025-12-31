using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class RootSegment(JsonNode root) : IJsonPathSegment
{
    public IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input)
    {
        yield return root;
    }
}
