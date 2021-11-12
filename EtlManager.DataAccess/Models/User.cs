using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

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

    [Required]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Required]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = null!;
}
