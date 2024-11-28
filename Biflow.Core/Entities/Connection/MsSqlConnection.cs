using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Entities.Scd;
using Biflow.Core.Entities.Scd.MsSql;

namespace Biflow.Core.Entities;

public class MsSqlConnection() : ConnectionBase(ConnectionType.Sql)
{
    public Guid? CredentialId { get; set; }

    public Credential? Credential { get; set; }

    [Display(Name = "Execute packages as login")]
    [MaxLength(128)]
    public string? ExecutePackagesAsLogin
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    [Range(0, int.MaxValue)]
    public int MaxConcurrentSqlSteps { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxConcurrentPackageSteps { get; set; }

    [JsonIgnore]
    public IEnumerable<AgentJobStep> AgentJobSteps { get; set; } = new List<AgentJobStep>();

    [JsonIgnore]
    public IEnumerable<PackageStep> PackageSteps { get; set; } = new List<PackageStep>();

    [JsonIgnore]
    public override IEnumerable<Step> Steps => AgentJobSteps.Cast<Step>().Concat(PackageSteps).Concat(SqlSteps);

    [JsonIgnore]
    public IEnumerable<MasterDataTable> DataTables { get; } = new List<MasterDataTable>();

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (Credential is not null && OperatingSystem.IsWindows())
        {
            await Credential.RunImpersonatedAsync(TestConnection);
        }
        else if (Credential is not null)
        {
            throw new ApplicationException("Impersonation is supported only on Windows.");
        }
        else
        {
            await TestConnection();
        }

        return;

        async Task TestConnection()
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs the provided delegate with impersonation using the <see cref="Credential"/> property if <see cref="CredentialId"/> is <see langword="not null"/>.
    /// Otherwise, the delegate will be run without impersonation.
    /// If <see cref="CredentialId"/> is not null but <see cref="Credential"/> is null, <see cref="ArgumentNullException"/> will be thrown.
    /// </summary>
    /// <param name="func">Delegate to be run</param>
    /// <returns><see cref="Task"/> that completes when the delegate completes</returns>
    public Task RunImpersonatedOrAsCurrentUserAsync(Func<Task> func)
    {
        if (CredentialId is null || !OperatingSystem.IsWindows())
        {
            return func();
        }
        ArgumentNullException.ThrowIfNull(Credential);
        return Credential.RunImpersonatedAsync(func);
    }

    /// <summary>
    /// Runs the provided delegate with impersonation using the <see cref="Credential"/> property if <see cref="CredentialId"/> is <see langword="not null"/>.
    /// Otherwise, the delegate will be run without impersonation.
    /// If <see cref="CredentialId"/> is not null but <see cref="Credential"/> is null, <see cref="ArgumentNullException"/> will be thrown.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func">Delegate to be run</param>
    /// <returns><see cref="Task"/> of <typeparamref name="T"/> that completes when the delegate completes</returns>
    public Task<T> RunImpersonatedOrAsCurrentUserAsync<T>(Func<Task<T>> func)
    {
        if (CredentialId is null || !OperatingSystem.IsWindows())
        {
            return func();
        }
        ArgumentNullException.ThrowIfNull(Credential);
        return Credential.RunImpersonatedAsync(func);
    }
    
    public override IColumnMetadataProvider CreateColumnMetadataProvider() =>
        new MsSqlColumnMetadataProvider(ConnectionString);
    
    public override IScdProvider CreateScdProvider(ScdTable table) =>
        new MsSqlScdProvider(table, CreateColumnMetadataProvider());
}
