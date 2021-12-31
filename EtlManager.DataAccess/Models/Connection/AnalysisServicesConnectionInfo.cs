namespace EtlManager.DataAccess.Models;

public class AnalysisServicesConnectionInfo : ConnectionInfoBase
{
    public AnalysisServicesConnectionInfo(string connectionName, string connectionString)
        : base(ConnectionType.AnalysisServices, connectionName, connectionString) { }

    public IList<TabularStep> TabularSteps { get; set; } = null!;

    public override IEnumerable<Step> Steps => TabularSteps?.Cast<Step>() ?? Enumerable.Empty<Step>();
}
