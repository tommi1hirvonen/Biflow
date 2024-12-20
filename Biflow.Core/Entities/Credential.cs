using Biflow.Core.Attributes;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public partial class Credential
{
    [JsonInclude]
    public Guid CredentialId { get; private set; }

    [MaxLength(200)]
    public string? Domain { get; set; }

    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = "";

    [JsonSensitive]
    public string? Password { get; set; }

    [JsonIgnore]
    public string DisplayName => Domain is { Length: > 0 } domain
        ? $@"{domain}\{Username}"
        : Username;

    private const int Logon32ProviderDefault = 0;
    private const int Logon32LogonInteractive = 2;

    [JsonIgnore]
    public IEnumerable<ExeStep> ExeSteps { get; private set; } = new List<ExeStep>();

    [JsonIgnore]
    public IEnumerable<MsSqlConnection> MsSqlConnections { get; private set; } = new List<MsSqlConnection>();
    
    [JsonIgnore]
    public IEnumerable<AnalysisServicesConnection> AnalysisServicesConnections { get; private set; } = new List<AnalysisServicesConnection>();

    [SupportedOSPlatform("windows")]
    public Task RunImpersonatedAsync(Func<Task> func)
    {
        if (!TryGetTokenHandle(out var token))
        {
            throw new ApplicationException("Could not get impersonation access token handle.");
        }
        return WindowsIdentity.RunImpersonatedAsync(token!, func);
    }

    [SupportedOSPlatform("windows")]
    public Task<T> RunImpersonatedAsync<T>(Func<Task<T>> func)
    {
        if (!TryGetTokenHandle(out var token))
        {
            throw new ApplicationException("Could not get impersonation access token handle.");
        }
        return WindowsIdentity.RunImpersonatedAsync(token!, func);
    }

    [SupportedOSPlatform("windows")]
    private bool TryGetTokenHandle([NotNullWhen(true)] out SafeAccessTokenHandle? token)
    {
        var domain = Domain;
        if (domain is null or { Length: 0 })
        {
            domain = ".";
        }
        if (LogonUserW(Username, domain, Password, Logon32LogonInteractive, Logon32ProviderDefault, out var handle))
        {
            token = new SafeAccessTokenHandle(handle);
            return true;
        }
        token = null;
        return false;
    }

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LogonUserW(
        string lpszUsername,
        string? lpszDomain,
        string? lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out nint phToken);
}
