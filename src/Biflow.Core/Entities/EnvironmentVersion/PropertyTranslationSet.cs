using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class PropertyTranslationSet
{
    public Guid PropertyTranslationSetId { get; init; }
    
    [Required]
    [MaxLength(250)]
    public string PropertyTranslationSetName { get; set; } = "";
    
    public IList<PropertyTranslation> PropertyTranslations { get; } = new List<PropertyTranslation>();
}