using FluentValidation;

namespace Biflow.Ui.Core;

public class ApiKeyValidator : AbstractValidator<ApiKey>
{
    public ApiKeyValidator()
    {
        RuleFor(k => k.ValidTo)
            .GreaterThan(k => k.ValidFrom)
            .WithMessage("'Valid to' must be greater than 'Valid from'");
    }
}
