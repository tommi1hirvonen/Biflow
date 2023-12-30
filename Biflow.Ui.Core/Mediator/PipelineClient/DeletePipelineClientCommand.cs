using MediatR;

namespace Biflow.Ui.Core;

public record DeletePipelineClientCommand(Guid PipelineClientId) : IRequest;