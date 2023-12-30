using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("User")]
public class User : IAuditable
{
    [Key]
    public Guid UserId { get; private set; }

    [Required]
    [MaxLength(250)]
    public required string Username { get; set; }

    [MaxLength(254)]
    [DataType(DataType.EmailAddress)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(500)]
    [Unicode(false)]
    public required List<string> Roles { get; init; }

    public bool AuthorizeAllJobs { get; set; }

    public bool AuthorizeAllDataTables { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = null!;

    public ICollection<Job> Jobs { get; set; } = null!;

    public ICollection<MasterDataTable> DataTables { get; set; } = null!;
}
