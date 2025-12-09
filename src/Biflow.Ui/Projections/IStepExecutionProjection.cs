namespace Biflow.Ui.Projections;

public interface IStepExecutionProjection
{
    public Guid ExecutionId { get; }
    
    public Guid StepId { get; }
    
    public int RetryAttemptIndex { get; }
    
    public string StepName { get; }
    
    public string JobName { get; }
    
    public StepType StepType { get; }
    
    public DisplayStepType DisplayStepType { get; }
    
    public DateTimeOffset? StartedOn { get; }
    
    public DateTimeOffset? EndedOn { get; }
    
    public StepExecutionStatus StepExecutionStatus { get; }
    
    public ExecutionStatus ExecutionStatus { get; }
    
    public int ExecutionPhase { get; }
    
    public ExecutionMode ExecutionMode { get; }
    
    public Guid[] Dependencies { get; }
    
    public IReadOnlyCollection<ITag> StepTags { get; }
    
    public double? ExecutionInSeconds => ((EndedOn ?? DateTime.Now) - StartedOn)?.TotalSeconds;
    
    public bool CanBeStopped =>
        StepExecutionStatus == StepExecutionStatus.Running
        || StepExecutionStatus == StepExecutionStatus.AwaitingRetry
        || StepExecutionStatus == StepExecutionStatus.Queued
        || StepExecutionStatus == StepExecutionStatus.NotStarted && ExecutionStatus == ExecutionStatus.Running;
    
    public bool IsSameAs(IStepExecutionProjection? other) =>
        ExecutionId == other?.ExecutionId &&
        StepId == other.StepId &&
        RetryAttemptIndex == other.RetryAttemptIndex;
}