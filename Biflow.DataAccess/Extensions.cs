namespace Biflow.DataAccess;

internal static class Extensions
{
    public static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}
