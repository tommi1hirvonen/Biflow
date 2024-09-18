using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class MasterDataTable
{
    [JsonInclude]
    public Guid DataTableId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string DataTableName { get; set; } = string.Empty;

    public string? DataTableDescription { get; set; }

    [Required]
    [MaxLength(128)]
    public string TargetSchemaName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string TargetTableName { get; set; } = string.Empty;

    [Required]
    public Guid ConnectionId { get; set; }

    public Guid? CategoryId { get; set; }

    public bool AllowInsert { get; set; } = true;

    public bool AllowDelete { get; set; } = true;

    public bool AllowUpdate { get; set; } = true;

    public bool AllowImport { get; set; } = true;

    public List<string> LockedColumns { get; set; } = [];

    public bool LockedColumnsExcludeMode { get; set; } = false;

    public List<string> HiddenColumns { get; set; } = [];

    public List<string> ColumnOrder { get; set; } = [];

    [JsonIgnore]
    public MasterDataTableCategory? Category { get; set; }

    [JsonIgnore]
    public MsSqlConnection Connection { get; set; } = null!;

    [JsonIgnore]
    public ICollection<User> Users { get; set; } = null!;

    [ValidateComplexType]
    [JsonInclude]
    public ICollection<MasterDataTableLookup> Lookups { get; private set; } = new List<MasterDataTableLookup>();

    [JsonIgnore]
    public IEnumerable<MasterDataTableLookup> DependentLookups { get; } = new List<MasterDataTableLookup>();

    [JsonIgnore]
    public byte[]? Timestamp { get; set; }
}