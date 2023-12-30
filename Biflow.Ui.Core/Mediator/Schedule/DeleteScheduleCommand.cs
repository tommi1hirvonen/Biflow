using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record DeleteScheduleCommand(Guid ScheduleId) : IRequest;