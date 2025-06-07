using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class MasterDataTableCategory
{
    public Guid CategoryId { get; init; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public IEnumerable<MasterDataTable> Tables { get; } = new List<MasterDataTable>();
}
