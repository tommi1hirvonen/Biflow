using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class DataTableLookup
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

    public DataTable DataTable { get; set; } = null!;

    public DataTable LookupDataTable { get; set; } = null!;
}
