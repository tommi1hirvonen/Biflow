using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record CreateConnectionCommand(ConnectionInfoBase Connection) : IRequest;