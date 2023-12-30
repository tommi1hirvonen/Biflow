using MediatR;

namespace Biflow.Ui.Core;

public record DeleteTagRequest(Guid TagId) : IRequest;