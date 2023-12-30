using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateExecutionRequest(Execution Execution) : IRequest;