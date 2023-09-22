using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("User")]
public class User
{
    [Key]
    [Required]
    [MaxLength(250)]
    public required string Username { get; set; }

    [MaxLength(254)]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Required]
    public required List<string> Roles { get; init; }

    public bool AuthorizeAllJobs { get; set; }

    public bool AuthorizeAllDataTables { get; set; }

    [Required]
    public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.UtcNow;

    [Required]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = null!;

    public ICollection<Job> Jobs { get; set; } = null!;

    public ICollection<MasterDataTable> DataTables { get; set; } = null!;
}
