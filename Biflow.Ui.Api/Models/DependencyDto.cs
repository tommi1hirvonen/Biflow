namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DependencyDto(Guid DependentOnStepId, DependencyType DependencyType);