using Biflow.Core.Attributes;
using Biflow.Core.Entities;
using System.Reflection;

namespace Biflow.Core;

public static class Extensions
{
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
}
