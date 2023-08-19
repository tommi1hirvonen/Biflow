namespace Biflow.Core;

public record CancelCommand(Guid? StepId, string Username);
