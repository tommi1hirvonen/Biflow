using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AnalysisServicesConnection() : ConnectionBase(ConnectionType.AnalysisServices)
{
    public Guid? CredentialId { get; set; }

    public Credential? Credential { get; set; }

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
