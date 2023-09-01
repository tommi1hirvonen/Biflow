using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight Step class replacement which can be used to only load selected attributes from the database using projection
/// </summary>
public record StepProjection(
    Guid StepId,
    Guid JobId,
    string? StepName,
    StepType StepType,
    int ExecutionPhase,
    bool IsEnabled,
    Tag[] Tags,
    Guid[] Dependencies) : IComparable
{
    public virtual bool Equals(StepProjection? other) =>
        other is not null && StepId == other.StepId;

    public override int GetHashCode() => base.GetHashCode();

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        if (obj is StepProjection other)
        {
            int result = ExecutionPhase.CompareTo(other.ExecutionPhase);
            if (result == 0)
            {
                return StepName?.CompareTo(other.StepName) ?? 0;
            }
            else
            {
                return result;
            }
        }
        else
        {
            throw new ArgumentException("Object is not a Step");
        }
    }
}
