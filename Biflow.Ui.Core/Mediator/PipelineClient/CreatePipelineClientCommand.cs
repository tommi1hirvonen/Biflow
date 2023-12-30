using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record CreatePipelineClientCommand(PipelineClient Client) : IRequest;