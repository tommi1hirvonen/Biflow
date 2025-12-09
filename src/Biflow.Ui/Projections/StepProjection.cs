namespace Biflow.Ui.Projections;

/// <summary>
/// Lightweight Step class replacement which can be used to only load selected attributes from the database using projection
/// </summary>
public record StepProjection(
    Guid StepId,
    Guid JobId,
    string JobName,
    string? StepName,
    StepType StepType,
    DisplayStepType DisplayStepType,
    int ExecutionPhase,
    bool IsEnabled,
    StepTag[] Tags,
    DependencyProjection[] Dependencies) : IComparable
{
    public virtual bool Equals(StepProjection? other) =>
        other is not null && StepId == other.StepId;

    public override int GetHashCode() => StepId.GetHashCode();

    public int CompareTo(object? obj)
    {
        switch (obj)
        {
            case null:
                return 1;
            case StepProjection other:
            {
                var result = ExecutionPhase.CompareTo(other.ExecutionPhase);
                return result == 0
                    ? StepName?.CompareTo(other.StepName) ?? 0
                    : result;
            }
            default:
                throw new ArgumentException("Object is not a Step");
        }
    }
}
