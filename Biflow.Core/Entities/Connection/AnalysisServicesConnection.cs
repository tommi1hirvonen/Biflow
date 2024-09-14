using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AnalysisServicesConnection() : ConnectionBase(ConnectionType.AnalysisServices)
{
    [JsonIgnore]
    public IEnumerable<TabularStep> TabularSteps { get; } = new List<TabularStep>();

    [JsonIgnore]
    public override IEnumerable<Step> Steps => TabularSteps?.Cast<Step>() ?? Enumerable.Empty<Step>();

    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        Task testConnection()
        {
            return Task.Run(() =>
            {
                using var server = new Microsoft.AnalysisServices.Tabular.Server();
                server.Connect(ConnectionString);
            }, cancellationToken);
        }
        if (Credential is not null && OperatingSystem.IsWindows())
        {
            await Credential.RunImpersonatedAsync(testConnection);
        }
        else if (Credential is not null)
        {
            throw new ApplicationException("Impersonation is supported only on Windows.");
        }
        else
        {
            await testConnection();
        }
    }
}
