namespace Biflow.Utilities;

public record CancelCommand(Guid? StepId, string Username);
