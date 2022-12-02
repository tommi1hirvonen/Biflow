using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class MasterDataTableLookup
{
    [Required]
    public Guid DataTableId { get; set; }

    [Required]
    [MaxLength(128)]
    public string ColumnName { get; set; } = "";

    [Required]
    public Guid LookupDataTableId { get; set; }

    [Required]
    [MaxLength(128)]
    public string LookupValueColumn { get; set; } = "";

    [Required]
    [MaxLength(128)]
    public string LookupDescriptionColumn { get; set; } = "";

    public LookupDisplayType LookupDisplayType { get; set; }

    public MasterDataTable Table { get; set; } = null!;

    public MasterDataTable LookupTable { get; set; } = null!;
}
