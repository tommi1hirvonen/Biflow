using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("DataTable")]
public class MasterDataTable
{
    [Key]
    public Guid DataTableId { get; set; }

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

    [Timestamp]
    public byte[]? Timestamp { get; set; }
}