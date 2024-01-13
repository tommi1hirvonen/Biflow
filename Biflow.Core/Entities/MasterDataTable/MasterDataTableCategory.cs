using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class MasterDataTableCategory
{
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public ICollection<MasterDataTable> Tables { get; set; } = null!;
}
