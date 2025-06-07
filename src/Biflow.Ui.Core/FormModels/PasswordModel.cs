using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Core;

public class PasswordModel
{
    [Required, ComplexPassword]
    public string Password { get; set; } = "";

    [Required]
    public string ConfirmPassword { get; set; } = "";
}
