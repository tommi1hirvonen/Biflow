using System;

namespace EtlManager.Utilities;

public record CancelCommand(Guid? StepId, string Username);
