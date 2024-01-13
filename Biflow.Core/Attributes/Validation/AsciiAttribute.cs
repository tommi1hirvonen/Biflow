using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Attributes.Validation;

public class AsciiAttribute : ValidationAttribute
{
    public AsciiAttribute() : base("Only ASCII characters allowed")
    {
    }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string str => str.All(char.IsAscii),
        _ => false
    };
}
