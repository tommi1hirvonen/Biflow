using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class AnalysisServicesConnection() : ConnectionBase(ConnectionType.AnalysisServices)
{
    public Guid? CredentialId { get; set; }

    public Credential? Credential { get; set; }

    [JsonIgnore]
    public IEnumerable<TabularStep> TabularSteps { get; set; } = new List<TabularStep>();

    public override IEnumerable<Step> Steps => TabularSteps;

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

        Task TestConnection()
        {
            return Task.Run(() =>
            {
                using var server = new Microsoft.AnalysisServices.Tabular.Server();
                server.Connect(ConnectionString);
            }, cancellationToken);
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
}
