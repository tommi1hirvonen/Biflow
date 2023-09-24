using Biflow.DataAccess.Models;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Core;

public record UserFormModel(User User, PasswordModel? PasswordModel);

public class PasswordModel
{
    [Required, ComplexPassword]
    public string Password { get; set; } = "";

    [Required]
    public string ConfirmPassword { get; set; } = "";
}
