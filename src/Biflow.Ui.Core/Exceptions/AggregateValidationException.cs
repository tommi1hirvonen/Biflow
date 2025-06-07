using System.ComponentModel.DataAnnotations;

namespace Biflow.Ui.Core;

public class AggregateValidationException(IReadOnlyList<ValidationResult> results) : Exception
{
    public IReadOnlyList<ValidationResult> ValidationResults => results;
}