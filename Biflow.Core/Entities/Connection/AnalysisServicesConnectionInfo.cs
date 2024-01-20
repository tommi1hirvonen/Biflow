using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AnalysisServicesConnectionInfo() : ConnectionInfoBase(ConnectionType.AnalysisServices)
{
    [JsonIgnore]
    public IEnumerable<TabularStep> TabularSteps { get; } = new List<TabularStep>();

    [JsonIgnore]
    public override IEnumerable<Step> Steps => TabularSteps?.Cast<Step>() ?? Enumerable.Empty<Step>();
}
