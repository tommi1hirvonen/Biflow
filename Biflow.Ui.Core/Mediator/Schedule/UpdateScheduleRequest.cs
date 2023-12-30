using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateScheduleRequest(Schedule Schedule, ICollection<string> Tags) : IRequest;