namespace Biflow.Core.Entities;

public class FabricStepExecutionAttempt : StepExecutionAttempt
{
    public FabricStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Fabric)
    {
        
    }

    public FabricStepExecutionAttempt(FabricStepExecutionAttempt other, int retryAttemptIndex)
        : base(other, retryAttemptIndex)
    {
        
    }

    public FabricStepExecutionAttempt(FabricStepExecution execution) : base(execution)
    {
        
    }
    
    public Guid JobInstanceId { get; set; }
}