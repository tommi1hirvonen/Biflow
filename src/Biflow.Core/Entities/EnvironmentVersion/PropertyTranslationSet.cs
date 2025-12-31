using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class PropertyTranslationSet
{
    public Guid PropertyTranslationSetId { get; init; }
    
    [Required]
    [MaxLength(250)]
    public string PropertyTranslationSetName { get; set; } = "";
    
    [JsonIgnore]
    public IList<PropertyTranslation> PropertyTranslations { get; } = new List<PropertyTranslation>();
}