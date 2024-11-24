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
        var result = SortOrder.CompareTo(other.SortOrder);
        return result == 0
            ? string.Compare(TagName, other.TagName, StringComparison.Ordinal)
            : result;
    }

    int IComparable.CompareTo(object? obj)
    {
        switch (obj)
        {
            case null:
                return 1;
            case ITag other:
                var result = SortOrder.CompareTo(other.SortOrder);
                return result == 0
                    ? string.Compare(TagName, other.TagName, StringComparison.Ordinal)
                    : result;
            default:
                throw new ArgumentException("Object is not an ITag");
        }
    }
}
