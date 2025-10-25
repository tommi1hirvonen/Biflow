using System.Text.RegularExpressions;

namespace Biflow.Core.Interfaces;

public interface IDataObject
{
    public string ObjectUri { get; }
    
    public bool UriEquals(IDataObject? other) => UriEquals(this, other);
    
    public static bool UriEquals(IDataObject? one, IDataObject? other) =>
        one is not null &&
        other is not null &&
        one.ObjectUri.Equals(other.ObjectUri); // case-sensitive

    public bool UriIsPartOf(IDataObject? other) => UriIsPartOf(this, other);
    
    public static bool UriIsPartOf(IDataObject? one, IDataObject? other) =>
        UriIsPartOf(one?.ObjectUri, other?.ObjectUri);
    
    public static bool UriIsPartOf(string? one, string? other)
    {
        if (one is null || other is null)
        {
            return false;
        }
        if (one.Equals(other))
        {
            return true;
        }
        if (one.StartsWith("tabular://") && other.StartsWith("tabular://"))
        {
            var tabularComponents = one.Split('/') switch
            {
                [.. var prefix, ""] => prefix,
                var x => x
            };
            var otherTabularComponents = other.Split('/') switch
            {
                [.. var prefix, ""] => prefix,
                var x => x
            };
            return tabularComponents
                .Zip(otherTabularComponents)
                .All(x => x.First == x.Second);
        }

        if (!one.StartsWith("blob://") || !other.StartsWith("blob://"))
        {
            return false;
        }
        
        var blobComponents = one.Split('/') switch
        {
            [.. var prefix, ""] => prefix,
            var x => x
        };
        var otherBlobComponents = other.Split('/') switch
        {
            [.. var prefix, ""] => prefix,
            var x => x
        };
        return blobComponents
            .Zip(otherBlobComponents)
            .All(x => x.First == x.Second || WildcardMatch(x.First, x.Second) || WildcardMatch(x.Second, x.First));
    }
    
    private static bool WildcardMatch(string pattern, string input) =>
        // Make sure the pattern is actually a wildcard pattern.
        (pattern.Contains('*') || pattern.Contains('?')) && Regex.IsMatch(input, WildCardToRegexPattern(pattern));
    
    private static string WildCardToRegexPattern(string value) =>
        "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
}