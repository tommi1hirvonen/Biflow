using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record CreateScheduleRequest(Schedule Schedule, ICollection<string> Tags) : IRequest;