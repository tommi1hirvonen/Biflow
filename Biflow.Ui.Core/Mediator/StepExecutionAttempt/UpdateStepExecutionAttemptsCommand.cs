using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateStepExecutionAttemptsCommand(IEnumerable<StepExecutionAttempt> Attempts) : IRequest;