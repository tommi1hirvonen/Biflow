namespace Biflow.DataAccess.Models;

public class AnalysisServicesConnectionInfo(string connectionName, string connectionString)
    : ConnectionInfoBase(ConnectionType.AnalysisServices, connectionName, connectionString)
{
    public IList<TabularStep> TabularSteps { get; set; } = null!;

    public override IEnumerable<Step> Steps => TabularSteps?.Cast<Step>() ?? Enumerable.Empty<Step>();
}
