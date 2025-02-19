using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Attributes.Validation;

public class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("Non-empty value required")
    {
    }
    
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        return IsValid(value)
            ? ValidationResult.Success!
            : new ValidationResult("Non-empty value required", [validationContext.MemberName ?? "NoMemberName"]);
    }

    public override bool IsValid(object? value) => 
        value is Guid guid && guid != Guid.Empty
        || value is string str && Guid.TryParse(str, out _);
}
