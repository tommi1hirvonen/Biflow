using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITag : IComparable<ITag>, IComparable
{
    public Guid TagId { get; }

    public string TagName { get; }

    public TagColor Color { get; }

    public int SortOrder { get; }

    int IComparable<ITag>.CompareTo(ITag? other)
    {
        if (other is null) return 1;
        int result = SortOrder.CompareTo(other.SortOrder);
        return result == 0
            ? TagName.CompareTo(other.TagName)
            : result;
    }

    int IComparable.CompareTo(object? obj)
    {
        if (obj is null) return 1;

        if (obj is ITag other)
        {
            int result = SortOrder.CompareTo(other.SortOrder);
            return result == 0
                ? TagName.CompareTo(other.TagName)
                : result;
        }
        else
        {
            throw new ArgumentException("Object is not an ITag");
        }
    }
}
