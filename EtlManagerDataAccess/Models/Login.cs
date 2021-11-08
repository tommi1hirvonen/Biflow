using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models;

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
