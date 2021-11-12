using System.Collections.Generic;

namespace EtlManager.DataAccess.Models;

public abstract class ParameterizedStepExecution : StepExecution
{
    public ParameterizedStepExecution(string stepName, StepType stepType) : base(stepName, stepType)
    {
    }

    public ICollection<StepExecutionParameterBase> StepExecutionParameters { get; set; } = null!;
}
