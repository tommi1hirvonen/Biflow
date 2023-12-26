using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("StepDataObject")]
[PrimaryKey("StepId", "ObjectId", "ReferenceType")]
public class StepDataObject
{
    public Guid StepId { get; set; }

    [JsonIgnore]
    public Step Step { get; set; } = null!;

    public Guid ObjectId { get; set; }

    [JsonIgnore]
    public DataObject DataObject { get; set; } = null!;

    [MaxLength(20)]
    [Unicode(false)]
    public DataObjectReferenceType ReferenceType { get; set; }

    [MaxLength(4000)]
    public List<string> DataAttributes { get; set; } = [];

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

        if (DataAttributes.Count == 0 || other.DataAttributes.Count == 0 || DataAttributes.Any(other.DataAttributes.Contains))
        {
            return true;
        }

        return false;
    }
}
