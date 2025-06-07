using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class Login
{

    [Required]
    [MaxLength(250)]
    public string? Username { get; set; }


    [Required]
    [MaxLength(250)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
