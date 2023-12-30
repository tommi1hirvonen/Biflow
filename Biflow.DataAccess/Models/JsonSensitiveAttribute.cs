namespace Biflow.DataAccess.Models;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsonSensitiveAttribute : Attribute
{
    public string? WhenContains { get; set; }

    public string Replacement { get; set; } = "";

    public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;
}