using FluentValidation;
using FluentValidation.Results;

namespace Biflow.Ui.Core;

public abstract class AsyncAbstractValidator<T> : AbstractValidator<T>
{
    private Task<ValidationResult>? validateTask;

    public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = default) =>
        validateTask = base.ValidateAsync(context, cancellation);

    public override ValidationResult Validate(ValidationContext<T> context)
    {
        var result = base.Validate(context);
        validateTask = Task.FromResult(result);
        return result;
    }

    public async Task<ValidationResult?> WaitForValidateAsync()
    {
        if (validateTask is not null)
        {
            return await validateTask;
        }

        return null;
    }

}