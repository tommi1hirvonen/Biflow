using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdatePipelineClientCommand(PipelineClient Client) : IRequest;