using MediatR;

namespace Biflow.Ui.Core;

public record ToggleScheduleCommand(Guid ScheduleId, bool IsEnabled) : IRequest;