namespace Biflow.DataAccess;

internal static class Utility
{
    public static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}
