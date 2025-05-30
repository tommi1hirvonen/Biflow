﻿using FluentValidation;

namespace Biflow.Ui.Core.Validation;

public class UserFormModelValidator : AbstractValidator<UserFormModel>
{
    public UserFormModelValidator(IEnumerable<string> reservedUsernames)
    {
        RuleFor(m => m.Username)
            .Must(n => !reservedUsernames.Contains(n))
            .WithMessage("Username already in use");
    }
}
