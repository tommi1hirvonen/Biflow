namespace Biflow.Ui.Projections;

public interface IExecutionProjection
{
    public string JobName { get; }

    public TagProjection[] JobTags { get; }

    public Guid? ScheduleId { get; }

    public ExecutionStatus ExecutionStatus { get; }
}
