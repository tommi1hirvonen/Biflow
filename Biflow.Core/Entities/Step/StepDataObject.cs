using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepDataObject
{
    public Guid StepId { get; init; }

    [JsonIgnore]
    public Step Step { get; init; } = null!;

    public Guid ObjectId { get; init; }

    [JsonIgnore]
    public DataObject DataObject { get; set; } = null!;

    public DataObjectReferenceType ReferenceType { get; init; }

    [MaxLength(4000)]
    public List<string> DataAttributes { get; init; } = [];

    public bool IsSubsetOf(StepDataObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (!DataObject.UriIsPartOf(other.DataObject))
        {
            return false;
        }

        return DataAttributes.Count == 0
               || other.DataAttributes.Count == 0
               || DataAttributes.Any(other.DataAttributes.Contains);
    }
}
