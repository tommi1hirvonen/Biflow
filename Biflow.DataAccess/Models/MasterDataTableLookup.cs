using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("DataTableLookup")]
public class MasterDataTableLookup
{
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
    [MinLength(1)]
    [MaxLength(128)]
    public string LookupValueColumn { get; set; } = "";

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string LookupDescriptionColumn { get; set; } = "";

    public LookupDisplayType LookupDisplayType { get; set; }

    public MasterDataTable Table { get; set; } = null!;

    public MasterDataTable LookupTable { get; set; } = null!;
}
