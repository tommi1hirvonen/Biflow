using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateStepExecutionAttemptsRequest(IEnumerable<StepExecutionAttempt> Attempts) : IRequest;