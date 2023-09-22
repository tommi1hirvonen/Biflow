namespace Biflow.Ui.Core;

public class PasswordValidationService
{
    public bool ValidatePassword(string password) =>
        Extensions.PasswordValidationRegex().Matches(password).Any();
}
