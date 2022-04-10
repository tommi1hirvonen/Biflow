using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class DataTable
{
    [Key]
    public Guid DataTableId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(250)]
    public string DataTableName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TargetSchemaName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TargetTableName { get; set; } = string.Empty;

    [Required]
    public Guid ConnectionId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    public ICollection<User> Users { get; set; } = null!;
}
