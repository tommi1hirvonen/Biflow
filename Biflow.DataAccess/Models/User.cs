using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class User
{
    [Key]
    [Required]
    [MaxLength(250)]
    public string? Username { get; set; }

    [MaxLength(254)]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Required]
    public string? Role { get; set; }

    public bool AuthorizeAllJobs { get; set; }

    public bool AuthorizeAllDataTables { get; set; }

    [Required]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Required]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = null!;

    public ICollection<Job> Jobs { get; set; } = null!;

    public ICollection<DataTable> DataTables { get; set; } = null!;
}
