using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class ExecutionDependency
{
    public ExecutionDependency() { }

    public ExecutionDependency(Dependency dependency, StepExecution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepId = dependency.StepId;
        StepExecution = execution;
        DependantOnStepId = dependency.DependantOnStepId;
        DependencyType = dependency.DependencyType;
    }

    public Guid ExecutionId { get; private set; }

    public Guid StepId { get; private set; }

    public Guid? DependantOnStepId { get; private set; }

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; private set; }

    public StepExecution StepExecution { get; set; } = null!;

    public StepExecution? DependantOnStepExecution { get; set; }
}
