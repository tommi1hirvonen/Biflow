namespace EtlManager.DataAccess.Models;

public class AnalysisServicesConnectionInfo : ConnectionInfoBase
{
    public AnalysisServicesConnectionInfo(string connectionName, string connectionString)
        : base(ConnectionType.AnalysisServices, connectionName, connectionString) { }

    public IList<TabularStep> Steps { get; set; } = null!;
}
