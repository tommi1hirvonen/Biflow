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
    public IList<SqlStep> SqlSteps { get; set; } = null!;

    [JsonIgnore]
    public IList<PackageStep> PackageSteps { get; set; } = null!;

    [JsonIgnore]
    public IList<AgentJobStep> AgentJobSteps { get; set; } = null!;

    [JsonIgnore]
    public IList<MasterDataTable> DataTables { get; set; } = null!;

    [JsonIgnore]
    public override IEnumerable<Step> Steps =>
        (SqlSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(PackageSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(AgentJobSteps?.Cast<Step>() ?? Enumerable.Empty<Step>());
}
