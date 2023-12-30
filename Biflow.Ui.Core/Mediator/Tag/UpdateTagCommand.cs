using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateTagCommand(Tag Tag) : IRequest;