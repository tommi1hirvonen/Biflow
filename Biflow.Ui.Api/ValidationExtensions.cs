using System.ComponentModel.DataAnnotations;
using FluentValidation;
using ValidationException = Biflow.Ui.Api.Exceptions.ValidationException;

namespace Biflow.Ui.Api;

internal static class ValidationExtensions
{
    internal static void EnsureDataAnnotationsValidated(this object model)
    {
        var (results, isValid) = model.ValidateDataAnnotations();
        if (!isValid)
        {
            throw new ValidationException(results);
        }
    }

    internal static void EnsureValidated<T>(this AbstractValidator<T> validator, T model)
    {
        if (validator.Validate(model) is not { IsValid: false, Errors: var errors })
        {
            return;
        }
        var results = errors.Select(e => new ValidationResult(e.ErrorMessage)).ToArray();
        throw new ValidationException(results);
    }
    
    private static (List<ValidationResult> Results, bool IsValid) ValidateDataAnnotations(this object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return (results, isValid);
    }

    internal static IDictionary<string, string[]> ToDictionary(this IEnumerable<ValidationResult> results)
    {
        var errors = results
            .SelectMany(result => result.MemberNames.Select(x => (MemberName: x, result.ErrorMessage)))
            .GroupBy(x => x.MemberName)
            .ToDictionary(
                key => key.Key,
                value => value.Select(x => x.ErrorMessage).OfType<string>().ToArray());
        return errors;
    }
}