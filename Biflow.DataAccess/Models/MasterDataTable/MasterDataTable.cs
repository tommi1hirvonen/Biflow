using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("DataTable")]
public class MasterDataTable
{
    [Key]
    [JsonInclude]
    public Guid DataTableId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string DataTableName { get; set; } = string.Empty;

    public string? DataTableDescription { get; set; }

    [Required]
    [MaxLength(128)]
    [Unicode(false)]
    public string TargetSchemaName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    [Unicode(false)]
    public string TargetTableName { get; set; } = string.Empty;

    [Required]
    public Guid ConnectionId { get; set; }

    [Column("DataTableCategoryId")]
    public Guid? CategoryId { get; set; }

    public bool AllowInsert { get; set; } = true;

    public bool AllowDelete { get; set; } = true;

    public bool AllowUpdate { get; set; } = true;

    public bool AllowImport { get; set; } = true;

    [MaxLength(8000)]
    [Unicode(false)]
    public List<string> LockedColumns { get; set; } = [];

    public bool LockedColumnsExcludeMode { get; set; } = false;

    [MaxLength(8000)]
    [Unicode(false)]
    public List<string> HiddenColumns { get; set; } = [];

    [MaxLength(8000)]
    [Unicode(false)]
    public List<string> ColumnOrder { get; set; } = [];

    [JsonIgnore]
    public MasterDataTableCategory? Category { get; set; }

    [JsonIgnore]
    public SqlConnectionInfo Connection { get; set; } = null!;

    [JsonIgnore]
    public ICollection<User> Users { get; set; } = null!;

    [ValidateComplexType]
    public ICollection<MasterDataTableLookup> Lookups { get; set; } = null!;

    [JsonIgnore]
    public ICollection<MasterDataTableLookup> DependentLookups { get; set; } = null!;

    [Timestamp]
    public byte[]? Timestamp { get; set; }
}