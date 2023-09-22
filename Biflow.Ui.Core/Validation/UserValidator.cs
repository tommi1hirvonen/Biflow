using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core.Validation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator(IEnumerable<string> reservedUsernames)
    {
        RuleFor(u => u.Username)
            .Must(n => !reservedUsernames.Contains(n))
            .WithMessage("Username already in use");
    }
}
