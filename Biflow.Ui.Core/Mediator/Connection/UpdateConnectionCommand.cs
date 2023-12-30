using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateConnectionCommand(ConnectionInfoBase Connection) : IRequest;