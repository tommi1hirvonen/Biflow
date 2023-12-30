using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core;

public record UserFormModel(User User, PasswordModel? PasswordModel);
