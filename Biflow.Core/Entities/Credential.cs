using Biflow.Core.Attributes;

namespace Biflow.Core.Entities;

public class Credential
{
    public Guid CredentialId { get; private set; }

    public string? Domain { get; set; }

    public string Username { get; set; } = "";

    [JsonSensitive]
    public string? Password { get; set; }

    public string DisplayName => Domain is { Length: > 0 } domain
        ? $@"{domain}\{Username}"
        : Username;
}
