using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal sealed class AndFilter(IJsonPathFilter left, IJsonPathFilter right) : IJsonPathFilter
{
    public bool Match(JsonNode node) => left.Match(node) && right.Match(node);
}

internal sealed class OrFilter(IJsonPathFilter left, IJsonPathFilter right) : IJsonPathFilter
{
    public bool Match(JsonNode node) => left.Match(node) || right.Match(node);
}

internal sealed class NotFilter(IJsonPathFilter inner) : IJsonPathFilter
{
    public bool Match(JsonNode node) => !inner.Match(node);
}
