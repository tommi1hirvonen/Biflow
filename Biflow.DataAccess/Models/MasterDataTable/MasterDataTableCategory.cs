using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("DataTableCategory")]
public class MasterDataTableCategory
{
    [Key]
    [Column("DataTableCategoryId")]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(250)]
    [MinLength(1)]
    [Column("DataTableCategoryName")]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public ICollection<MasterDataTable> Tables { get; set; } = null!;
}
