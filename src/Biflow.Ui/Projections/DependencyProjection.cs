namespace Biflow.Ui.Projections;

public record DependencyProjection(Guid StepId, Guid DependentOnStepId, DependencyType DependencyType);