using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class MsSqlConnection() : ConnectionBase(ConnectionType.Sql)
{
    public Guid? CredentialId { get; set; }

    public Credential? Credential { get; set; }

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

    /// <summary>
    /// Runs the provided delegate with impersonation using the <see cref="Credential"/> property if <see cref="CredentialId"/> is <see langword="not null"/>.
    /// Otherwise the delegate will be run without impersonation.
    /// If <see cref="CredentialId"/> is not null but <see cref="Credential"/> is null, <see cref="ArgumentNullException"/> will be thrown.
    /// </summary>
    /// <param name="func">Delegate to be run</param>
    /// <returns><see cref="Task"/> that completes when the delegate completes</returns>
    public Task RunImpersonatedOrAsCurrentUserAsync(Func<Task> func)
    {
        if (CredentialId is not null && OperatingSystem.IsWindows())
        {
            ArgumentNullException.ThrowIfNull(Credential);
            return Credential.RunImpersonatedAsync(func);
        }
        return func();
    }

    /// <summary>
    /// Runs the provided delegate with impersonation using the <see cref="Credential"/> property if <see cref="CredentialId"/> is <see langword="not null"/>.
    /// Otherwise the delegate will be run without impersonation.
    /// If <see cref="CredentialId"/> is not null but <see cref="Credential"/> is null, <see cref="ArgumentNullException"/> will be thrown.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func">Delegate to be run</param>
    /// <returns><see cref="Task"/> of <typeparamref name="T"/> that completes when the delegate completes</returns>
    public Task<T> RunImpersonatedOrAsCurrentUserAsync<T>(Func<Task<T>> func)
    {
        if (CredentialId is not null && OperatingSystem.IsWindows())
        {
            ArgumentNullException.ThrowIfNull(Credential);
            return Credential.RunImpersonatedAsync(func);
        }
        return func();
    }
}
