using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Attributes.Validation;

public class AsciiAttribute() : ValidationAttribute("Only ASCII characters allowed")
{
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string str => str.All(char.IsAscii),
        _ => false
    };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext) =>
        IsValid(value) switch
        {
            true => ValidationResult.Success!,
            false => new ValidationResult("Only ASCII characters allowed",
                [validationContext.MemberName ?? "NoMemberName"])
        };
}
