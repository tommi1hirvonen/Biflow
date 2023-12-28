using MediatR;

namespace Biflow.Ui.Core;

public record DeleteJobRequest(Guid JobId) : IRequest;
