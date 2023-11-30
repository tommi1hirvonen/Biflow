using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class UriAttribute : ValidationAttribute
{
    public UriAttribute() : base("Valid Uri required")
    {
    }

    public override bool IsValid(object? value) =>
        value is string str && Uri.IsWellFormedUriString(str, UriKind.RelativeOrAbsolute);
}
