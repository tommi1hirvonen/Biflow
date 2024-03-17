using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class Credential
{
    public Guid CredentialId { get; private set; }

    [MaxLength(200)]
    public string? Domain { get; set; }

    [Required]
    [MaxLength(200)]
    public string Username { get; set; } = "";

    [JsonSensitive]
    public string? Password { get; set; }

    public string DisplayName => Domain is { Length: > 0 } domain
        ? $@"{domain}\{Username}"
        : Username;
}
