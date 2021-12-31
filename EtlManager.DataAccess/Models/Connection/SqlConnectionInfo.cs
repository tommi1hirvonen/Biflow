using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class SqlConnectionInfo : ConnectionInfoBase
{

    public SqlConnectionInfo(string connectionName, string connectionString)
        : base(ConnectionType.Sql, connectionName, connectionString) { }

    [Display(Name = "Execute packages as login")]
    public string? ExecutePackagesAsLogin
    {
        get => _executePackagesAsLogin;
        set => _executePackagesAsLogin = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _executePackagesAsLogin;

    public IList<SqlStep> SqlSteps { get; set; } = null!;

    public IList<PackageStep> PackageSteps { get; set; } = null!;

    public IList<AgentJobStep> AgentJobSteps { get; set; } = null!;

    public override IEnumerable<Step> Steps =>
        (SqlSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(PackageSteps?.Cast<Step>() ?? Enumerable.Empty<Step>())
        .Concat(AgentJobSteps?.Cast<Step>() ?? Enumerable.Empty<Step>());
}
