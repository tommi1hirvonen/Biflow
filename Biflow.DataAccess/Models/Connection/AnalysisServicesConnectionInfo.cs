using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class AnalysisServicesConnectionInfo(string connectionName, string connectionString)
    : ConnectionInfoBase(ConnectionType.AnalysisServices, connectionName, connectionString)
{
    [JsonIgnore]
    public IList<TabularStep> TabularSteps { get; set; } = null!;

    [JsonIgnore]
    public override IEnumerable<Step> Steps => TabularSteps?.Cast<Step>() ?? Enumerable.Empty<Step>();
}
