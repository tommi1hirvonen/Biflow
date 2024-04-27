using Biflow.Core.Attributes;
using Biflow.Core.Entities;
using System.Collections;
using System.Reflection;

namespace Biflow.Core;

public static class Extensions
{
    public static string? NullIfEmpty(this string? value) => string.IsNullOrEmpty(value) ? null : value;

    public static string? GetDurationInReadableFormat(this StepExecutionAttempt attempt) => attempt.ExecutionInSeconds?.SecondsToReadableFormat();

    public static string? GetDurationInReadableFormat(this Execution execution) => execution.ExecutionInSeconds?.SecondsToReadableFormat();

    public static string SecondsToReadableFormat(this double value)
    {
        var duration = TimeSpan.FromSeconds(value);
        var result = "";
        var days = duration.Days;
        var hours = duration.Hours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;
        if (days > 0) result += days + " d ";
        if (hours > 0 || days > 0) result += hours + " h ";
        if (minutes > 0 || hours > 0 || days > 0) result += minutes + " min ";
        result += seconds + " s";
        return result;
    }

    public static (string Name, int Ordinal)? GetCategory(this StepType value)
    {
        var name = Enum.GetName(value);
        if (name is null)
        {
            return null;
        }
        var category = typeof(StepType).GetField(name)?.GetCustomAttributes<CategoryAttribute>().FirstOrDefault();
        return category is not null
            ? (category.Name, category.Ordinal)
            : null;
    }

    public static string? GetDescription(this StepType value)
    {
        var name = Enum.GetName(value);
        if (name is null)
        {
            return null;
        }
        var description = typeof(StepType).GetField(name)?.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
        return description?.Text;
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        if (collection is List<T> list)
        {
            list.AddRange(items);
            return;
        }

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
    {
        foreach (var item in enumerable)
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    public static void SortBy<T, U>(this IList<T> list, Func<T, U> propertyDelegate) where U : IComparable =>
        list.Sort((one, other) => propertyDelegate(one).CompareTo(propertyDelegate(other)));

    public static void Sort<T>(this IList<T> list, Comparison<T> comparison) =>
        ArrayList.Adapter((IList)list).Sort(Comparer<T>.Create(comparison));
}
