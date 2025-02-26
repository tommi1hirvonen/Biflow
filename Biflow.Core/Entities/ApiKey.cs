using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Biflow.Core.Entities;

public class ApiKey(string? value = null) : IAuditable
{
    public Guid Id { get; init; }

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = "";

    public string Value { get; private set; } = value ?? GenerateApiKey();

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ValidTo { get; set; }

    public bool IsRevoked { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    public string? LastModifiedBy { get; set; }
    
    public List<string> Scopes { get; init; } = [];

    public void ToggleScope(string scope)
    {
        if (Scopes.Remove(scope))
        {
            return;
        }
        if (!Constants.Scopes.AsReadOnlyList().Contains(scope))
        {
            return;
        }
        Scopes.Add(scope);
        Scopes.Sort();
    }

    private static string GenerateApiKey()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .Replace("/", "")
            .Replace("+", "");
    }
}
