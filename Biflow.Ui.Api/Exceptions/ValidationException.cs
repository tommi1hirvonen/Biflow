using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Api.Exceptions;

public class ValidationException(IReadOnlyList<ValidationResult> results) : Exception
{
    public IReadOnlyList<ValidationResult> ValidationResults => results;
}