using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Biflow.Core.Entities;

public partial class DataObject : IDataObject
{
    [JsonInclude]
    public Guid ObjectId { get; private set; }

    [Required]
    [Ascii]
    [MaxLength(500)]
    public string ObjectUri { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public int MaxConcurrentWrites { get; set; } = 1;

    [JsonIgnore]
    public DataObjectMappingResult SourceMappingResult { get; init; } = new();
    
    [JsonIgnore]
    public DataObjectMappingResult TargetMappingResult { get; init; } = new();

    [JsonIgnore]
    public ICollection<StepDataObject> Steps { get; } = new List<StepDataObject>();

    public bool UriEquals(IDataObject? other) =>
        other is not null &&
        ObjectUri.Equals(other.ObjectUri); // case sensitive

    public bool UriIsPartOf(IDataObject? other)
    {
        if (other is null)
        {
            return false;
        }
        if (UriEquals(other))
        {
            return true;
        }
        if (ObjectUri.StartsWith("tabular://") && other.ObjectUri.StartsWith("tabular://"))
        {
            var tabularComponents = ObjectUri.Split('/') switch
            {
                [.. var prefix, ""] => prefix,
                var x => x
            };
            var otherTabularComponents = other.ObjectUri.Split('/') switch
            {
                [.. var prefix, ""] => prefix,
                var x => x
            };
            return tabularComponents
                .Zip(otherTabularComponents)
                .All(x => x.First == x.Second);
        }

        if (!ObjectUri.StartsWith("blob://") || !other.ObjectUri.StartsWith("blob://"))
        {
            return false;
        }
        
        var blobComponents = ObjectUri.Split('/') switch
        {
            [.. var prefix, ""] => prefix,
            var x => x
        };
        var otherBlobComponents = other.ObjectUri.Split('/') switch
        {
            [.. var prefix, ""] => prefix,
            var x => x
        };
        return blobComponents
            .Zip(otherBlobComponents)
            .All(x => x.First == x.Second || WildcardMatch(x.First, x.Second) || WildcardMatch(x.Second, x.First));
    }

    [JsonIgnore]
    public bool IsValid => ObjectUri.All(char.IsAscii);

    public static string CreateTableUri(string server, string database, string schema, string table)
    {
        server = NonAsciiCharsRegex.Replace(server, Replacement);
        database = NonAsciiCharsRegex.Replace(database, Replacement);
        schema = NonAsciiCharsRegex.Replace(schema, Replacement);
        table = NonAsciiCharsRegex.Replace(table, Replacement);
        return $"table://{server}/{database}/{schema}/{table}";
    }

    public static string CreateDatasetUri(string workspaceName, string datasetName)
    {
        workspaceName = NonAsciiCharsRegex.Replace(workspaceName, Replacement);
        datasetName = NonAsciiCharsRegex.Replace(datasetName, Replacement);
        return $"pbi://{workspaceName}/{datasetName}";
    }

    public static string CreateTabularUri(string server, string model, string? table, string? partition)
    {
        model = NonAsciiCharsRegex.Replace(model, Replacement);
        if (partition is not null && table is not null)
        {
            table = NonAsciiCharsRegex.Replace(table, Replacement);
            partition = NonAsciiCharsRegex.Replace(partition, Replacement);
            return $"tabular://{server}/{model}/{table}/{partition}";
        }

        if (table is null)
        {
            return $"tabular://{server}/{model}";
        }
        
        table = NonAsciiCharsRegex.Replace(table, Replacement);
        return $"tabular://{server}/{model}/{table}";
    }

    public static string CreateQlikUri(string appName)
    {
        appName = NonAsciiCharsRegex.Replace(appName, Replacement);
        return $"qlik://{appName}";
    }

    public static string CreateBlobUri(string accountName, string containerName, string blobPath)
    {
        blobPath = NonAsciiCharsRegex.Replace(blobPath, Replacement);
        return $"blob://{accountName}/{containerName}/{blobPath}";
    }

    private static bool WildcardMatch(string pattern, string input) =>
        // Make sure the pattern is actually a wildcard pattern.
        (pattern.Contains('*') || pattern.Contains('?')) && Regex.IsMatch(input, WildCardToRegexPattern(pattern));
    private static string WildCardToRegexPattern(string value) =>
        "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";

    private const string Replacement = "_";

    [GeneratedRegex(@"[^\u0000-\u007F]+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NonAsciiCharsRegex { get; }
}
