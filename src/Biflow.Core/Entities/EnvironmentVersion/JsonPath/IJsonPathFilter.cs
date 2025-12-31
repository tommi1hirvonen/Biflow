using System.Text.Json.Nodes;

namespace Biflow.Core.Entities;

internal interface IJsonPathFilter
{
    bool Match(JsonNode node);
}