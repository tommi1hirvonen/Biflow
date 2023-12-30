using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record UpdateScheduleCommand(Schedule Schedule, ICollection<string> Tags) : IRequest;