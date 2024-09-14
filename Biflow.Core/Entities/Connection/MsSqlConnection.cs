using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class MsSqlConnection() : ConnectionBase(ConnectionType.Sql)
{
    [Display(Name = "Execute packages as login")]
    [MaxLength(128)]
    public string? ExecutePackagesAsLogin
    {
        get => _executePackagesAsLogin;
        set => _executePackagesAsLogin = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _executePackagesAsLogin;

    [JsonIgnore]
    public IEnumerable<SqlStep> SqlSteps { get; } = new List<SqlStep>();

    [JsonIgnore]
    public IEnumerable<PackageStep> PackageSteps { get; } = new List<PackageStep>();

    [JsonIgnore]
    public IEnumerable<AgentJobStep> AgentJobSteps { get; } = new List<AgentJobStep>();

    [JsonIgnore]
    public IEnumerable<MasterDataTable> DataTables { get; } = new List<MasterDataTable>();

    [JsonIgnore]
    public override IEnumerable<Step> Steps =>
        (SqlSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(PackageSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(AgentJobSteps?.Cast<Step>() ?? Enumerable.Empty<Step>());

    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        async Task testConnection()
        {
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);
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
