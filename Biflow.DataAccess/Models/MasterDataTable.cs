using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("DataTable")]
public class MasterDataTable
{
    [Key]
    public Guid DataTableId { get; private set; }

    [Required]
    [MinLength(1)]
    [MaxLength(250)]
    public string DataTableName { get; set; } = string.Empty;

    public string? DataTableDescription { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TargetSchemaName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TargetTableName { get; set; } = string.Empty;

    public bool AllowInsert { get; set; } = true;

    public bool AllowDelete { get; set; } = true;

    public bool AllowUpdate { get; set; } = true;

    public bool AllowImport { get; set; } = true;

    [Required]
    public Guid ConnectionId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    [Column("DataTableCategoryId")]
    public Guid? CategoryId { get; set; }

    public MasterDataTableCategory? Category { get; set; }

    public ICollection<User> Users { get; set; } = null!;

    [ValidateComplexType]
    public ICollection<MasterDataTableLookup> Lookups { get; set; } = null!;

    public ICollection<MasterDataTableLookup> DependentLookups { get; set; } = null!;

    public List<string> LockedColumns { get; set; } = new();

    public bool LockedColumnsExcludeMode { get; set; } = false;

    [Timestamp]
    public byte[]? Timestamp { get; set; }
}