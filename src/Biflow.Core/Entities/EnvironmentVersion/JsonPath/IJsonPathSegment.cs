using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal interface IJsonPathSegment
{
    IEnumerable<JsonNode?> Evaluate(IEnumerable<JsonNode?> input);
}
