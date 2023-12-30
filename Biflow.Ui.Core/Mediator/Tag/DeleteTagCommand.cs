using MediatR;

namespace Biflow.Ui.Core;

public record DeleteTagCommand(Guid TagId) : IRequest;