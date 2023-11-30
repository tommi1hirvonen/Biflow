using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("Non-empty Guid required")
    {
    }

    public override bool IsValid(object? value) => value is Guid guid && guid != Guid.Empty;
}
