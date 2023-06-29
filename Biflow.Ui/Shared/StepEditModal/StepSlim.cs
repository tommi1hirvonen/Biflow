using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared.StepEditModal;

/// <summary>
/// Lightweight Step class replacement which can be used to only load selected attributes from the database
/// </summary>
/// <param name="StepId"></param>
/// <param name="JobId"></param>
/// <param name="StepName"></param>
/// <param name="StepType"></param>
/// <param name="ExecutionPhase"></param>
/// <param name="Tags"></param>
/// <param name="Dependencies"></param>
public record StepSlim(
    Guid StepId,
    Guid JobId,
    string? StepName,
    StepType StepType,
    int ExecutionPhase,
    IList<Tag> Tags,
    IList<Guid> Dependencies) : IComparable
{
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        if (obj is StepSlim other)
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
