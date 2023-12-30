using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record CreateUserCommand(User User, PasswordModel PasswordModel) : IRequest;