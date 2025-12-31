using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class JsonPathEvaluator(IEnumerable<IJsonPathSegment> segments)
{
    private readonly List<IJsonPathSegment> _segments = segments.ToList();

    public IEnumerable<JsonNode?> Evaluate(JsonNode root)
    {
        IEnumerable<JsonNode?> current = [root];

        foreach (var segment in _segments)
        {
            current = segment.Evaluate(current).ToList();
        }

        return current;
    }
}
