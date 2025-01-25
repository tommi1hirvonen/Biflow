using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Biflow.Ui.Core;

public static class ValidationExtensions
{
    public static void EnsureDataAnnotationsValidated(this object model)
    {
        var (results, isValid) = model.ValidateDataAnnotations();
        if (!isValid)
        {
            throw new AggregateValidationException(results);
        }
    }

    public static async Task EnsureValidatedAsync<T>(this AbstractValidator<T> validator, T model,
        CancellationToken cancellationToken = default)
    {
        if (await validator.ValidateAsync(model, cancellationToken) is not { IsValid: false, Errors: var errors })
        {
            return;
        }
        var results = errors.Select(e => new ValidationResult(e.ErrorMessage)).ToArray();
        throw new AggregateValidationException(results);
    }
    
    private static (List<ValidationResult> Results, bool IsValid) ValidateDataAnnotations(this object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return (results, isValid);
    }
}