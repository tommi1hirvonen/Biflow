using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record DeleteScheduleRequest(Guid ScheduleId) : IRequest;