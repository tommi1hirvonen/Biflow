using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    public Guid DependantOnStepId { get; private set; }

    [Display(Name = "Type")]
    public DependencyType DependencyType { get; private set; }

    [JsonIgnore]
    public StepExecution StepExecution { get; private set; } = null!;
}
