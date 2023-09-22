using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Core;

public class ComplexPasswordAttribute : RegularExpressionAttribute
{
    public ComplexPasswordAttribute()
        : base(@"^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[*.!@#$%^&(){}[\]:;<>,.?/~_+-=|]).{8,32}$")
    {
        ErrorMessage = "The password must contain 8-32 characters, at least one uppercase letter, one lowercase letter, one number and one special character";
    }
}
