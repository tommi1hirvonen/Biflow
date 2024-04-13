using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(SqlConnectionInfo), nameof(ConnectionType.Sql))]
[JsonDerivedType(typeof(AnalysisServicesConnectionInfo), nameof(ConnectionType.AnalysisServices))]
public abstract class ConnectionInfoBase(ConnectionType connectionType) : IComparable
{
    [Display(Name = "Connection id")]
    [JsonInclude]
    public Guid ConnectionId { get; private set; }

    [Display(Name = "Connection type")]
    public ConnectionType ConnectionType { get; } = connectionType;

    [Required]
    [MaxLength(250)]
    [Display(Name = "Connection name")]
    public string ConnectionName { get; set; } = "";

    [Required]
    [Display(Name = "Connection string")]
    [JsonSensitive(WhenContains = "password")]
    public string ConnectionString { get; set; } = "";

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        ConnectionInfoBase connection => -connection.ConnectionName.CompareTo(ConnectionName),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [JsonIgnore]
    public abstract IEnumerable<Step> Steps { get; }

    public Guid? CredentialId { get; set; }

    public Credential? Credential { get; set; }

    public abstract Task TestConnectionAsync(CancellationToken cancellationToken = default);

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
