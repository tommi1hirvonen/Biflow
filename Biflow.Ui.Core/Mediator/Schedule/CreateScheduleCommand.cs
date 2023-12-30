using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record CreateScheduleCommand(Schedule Schedule, ICollection<string> Tags) : IRequest;