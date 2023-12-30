namespace Biflow.Ui.SqlServer;

internal static class Extensions
{
    internal static string EncodeForLike(this string term) => term.Replace("[", "[[]").Replace("%", "[%]");
}
