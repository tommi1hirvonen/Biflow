using FluentValidation;
using FluentValidation.Results;

namespace Biflow.Ui.Core;

public abstract class AsyncAbstractValidator<T> : AbstractValidator<T>
{
    private Task<ValidationResult>? _validateTask;

    public override Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken cancellation = default) =>
        _validateTask = base.ValidateAsync(context, cancellation);

    public override ValidationResult Validate(ValidationContext<T> context)
    {
        var result = base.Validate(context);
        _validateTask = Task.FromResult(result);
        return result;
    }

    public async Task<ValidationResult?> WaitForValidateAsync()
    {
        if (_validateTask is not null)
        {
            return await _validateTask;
        }

        return null;
    }

}