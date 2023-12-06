using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Biflow.DataAccess.Models;

[Table("DataObject")]
public partial class DataObject : IDataObject
{
    [Key]
    public Guid ObjectId { get; private set; }

    [Required]
    [Ascii]
    [MaxLength(500)]
    public string ObjectUri { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public int MaxConcurrentWrites { get; set; } = 1;

    [NotMapped] public DataObjectMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public DataObjectMappingResult TargetMappingResult { get; set; } = new();

    public IList<StepDataObject> Steps { get; set; } = null!;

    public bool UriEquals(IDataObject? other) =>
        other is not null &&
        ObjectUri.EqualsIgnoreCase(other.ObjectUri);

    public bool IsValid => ObjectUri.All(char.IsAscii);

    public static string CreateTableUri(string server, string database, string schema, string table)
    {
        server = NonAsciiCharsRegex().Replace(server, Replacement);
        database = NonAsciiCharsRegex().Replace(database, Replacement);
        schema = NonAsciiCharsRegex().Replace(schema, Replacement);
        table = NonAsciiCharsRegex().Replace(table, Replacement);
        return $"table://{server}/{database}/{schema}/{table}";
    }

    public static string CreateDatasetUri(string workspaceName, string datasetName)
    {
        workspaceName = NonAsciiCharsRegex().Replace(workspaceName, Replacement);
        datasetName = NonAsciiCharsRegex().Replace(datasetName, Replacement);
        return $"pbi://{workspaceName}/{datasetName}";
    }

    public static string CreateTabularUri(string server, string model, string? table, string? partition)
    {
        model = NonAsciiCharsRegex().Replace(model, Replacement);
        if (partition is not null && table is not null)
        {
            table = NonAsciiCharsRegex().Replace(table, Replacement);
            partition = NonAsciiCharsRegex().Replace(partition, Replacement);
            return $"tabular://{server}/{model}/{table}/{partition}";
        }
        else if (table is not null)
        {
            table = NonAsciiCharsRegex().Replace(table, Replacement);
            return $"tabular://{server}/{model}/{table}";
        }

        return $"tabular://{server}/{model}";
    }

    public static string CreateQlikUri(string appName)
    {
        appName = NonAsciiCharsRegex().Replace(appName, Replacement);
        return $"qlik://{appName}";
    }

    public static string CreateBlobUri(string accountName, string containerName, string blobPath)
    {
        blobPath = NonAsciiCharsRegex().Replace(blobPath, Replacement);
        return $"blob://{accountName}/{containerName}/{blobPath}";
    }

    private const string Replacement = "_";

    [GeneratedRegex(@"[^\u0000-\u007F]+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NonAsciiCharsRegex();
}

public class DataObjectMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}

public interface IDataObject
{
    public string ObjectUri { get; }
}