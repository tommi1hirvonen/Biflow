using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class Login
{

    [Required]
    [MaxLength(250)]
    [Display(Name = "Username")]
    public string? Username { get; set; }


    [Required]
    [MaxLength(250)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }
}
