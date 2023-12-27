using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("DataTableLookup")]
public class MasterDataTableLookup
{
    [Key]
    public Guid LookupId { get; set; }

    [Required]
    [Column("DataTableId")]
    public Guid TableId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ColumnName { get; set; } = "";

    [Required]
    [Column("LookupDataTableId")]
    public Guid LookupTableId { get; set; }

    [Required]
    [MaxLength(128)]
    public string LookupValueColumn { get; set; } = "";

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string LookupDescriptionColumn { get; set; } = "";

    public LookupDisplayType LookupDisplayType { get; set; }

    [JsonIgnore]
    public MasterDataTable Table { get; set; } = null!;

    [JsonIgnore]
    public MasterDataTable LookupTable { get; set; } = null!;
}
