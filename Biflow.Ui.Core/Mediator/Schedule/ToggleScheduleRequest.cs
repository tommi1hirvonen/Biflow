using MediatR;

namespace Biflow.Ui.Core;

public record ToggleScheduleRequest(Guid ScheduleId, bool IsEnabled) : IRequest;