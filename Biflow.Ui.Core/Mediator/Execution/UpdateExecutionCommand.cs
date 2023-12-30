using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateExecutionCommand(Execution Execution) : IRequest;