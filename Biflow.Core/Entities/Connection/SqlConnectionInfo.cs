using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class SqlConnectionInfo() : ConnectionInfoBase(ConnectionType.Sql)
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
}
