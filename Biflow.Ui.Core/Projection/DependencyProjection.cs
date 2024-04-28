namespace Biflow.Ui.Core.Projection;

public record DependencyProjection(Guid StepId, Guid DependentOnStepId, DependencyType DependencyType);