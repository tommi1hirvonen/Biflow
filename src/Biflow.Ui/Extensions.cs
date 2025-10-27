using System.Runtime.CompilerServices;
using CronExpressionDescriptor;
using Quartz;
using ParameterValue = Biflow.Core.Entities.ParameterValue;
using StartEnd = (System.DateTimeOffset? Start, System.DateTimeOffset? End);

namespace Biflow.Ui;

public static class Extensions
{
    /// <summary>
    /// Convenience method for toggling on/off <see cref="DynamicParameter.UseExpression"/>.
    /// When the value is set to false,
    /// the <see cref="ParameterValue"/> will be generated with the provided <see cref="ParameterValueType"/>. 
    /// </summary>
    /// <param name="parameter">The parameter for which to toggle <see cref="DynamicParameter.UseExpression"/></param>
    /// <param name="valueType">The type to use for the default value</param>
    public static void ToggleUseExpression(this DynamicParameter parameter, ParameterValueType valueType)
    {
        parameter.UseExpression = !parameter.UseExpression;
        if (!parameter.UseExpression)
        {
            parameter.ParameterValue = ParameterValue.DefaultValue(valueType);
        }
    }
    
    /// <summary>
    /// Calculate Gantt graph dimensions for a tuple of DateTimeOffsets (start and end time). The start and end time are compared to the list of all tuples provided as an argument.
    /// The method assumes constant width of 100 for the Gantt graph.
    /// </summary>
    /// <param name="execution"></param>
    /// <param name="allExecutions">List of all executions (start and end times) shown on the Gantt graph</param>
    /// <returns>Offset (between 0 and 99) and width (between 1 and 100) of the element in the Gantt graph</returns>
    public static (double Offset, double Width) GetGanttGraphDimensions(this StartEnd execution, IEnumerable<StartEnd> allExecutions)
    {
        var executions = allExecutions.ToArray();
        if (executions.Length == 0)
        {
            return (0, 0);
        }

        var minTime = executions.Min(e => e.Start?.LocalDateTime) ?? DateTime.Now;
        var maxTime = executions.Max(e => e.End?.LocalDateTime ?? DateTime.Now);

        var minTicks = minTime.Ticks;
        var maxTicks = maxTime.Ticks;

        if (minTicks == maxTicks)
        {
            return (0, 0);
        }

        var startTicks = (execution.Start?.LocalDateTime ?? DateTime.Now).Ticks;
        var endTicks = (execution.End?.LocalDateTime ?? DateTime.Now).Ticks;

        var start = (double)(startTicks - minTicks) / (maxTicks - minTicks) * 100;
        var end = (double)(endTicks - minTicks) / (maxTicks - minTicks) * 100;
        var width = end - start;
        width = width < 1 ? 1 : width; // check that width is not 0
        start = start > 99 ? 99 : start; // check that start is not 100

        return (start, width);
    }

    /// <summary>
    /// Get a string describing the schedule's underlying Cron expression
    /// </summary>
    /// <returns>Descriptive text if the Cron expression is valid. Otherwise, an error message string is returned.</returns>
    public static string GetScheduleDescription(this Schedule schedule) =>
        GetCronExpressionDescription(schedule.CronExpression);

    /// <summary>
    /// Get a string describing a Cron expression
    /// </summary>
    /// <param name="expression">String to read as Cron expression</param>
    /// <returns>Descriptive text if the Cron expression is valid. Otherwise, an error message string is returned.</returns>
    public static string GetCronExpressionDescription(string? expression)
    {
        if (expression is not null && CronExpression.IsValidExpression(expression))
        {
            return ExpressionDescriptor.GetDescription(expression, new Options
            {
                ThrowExceptionOnParseError = false,
                Use24HourTimeFormat = true,
                Locale = "en",
                DayOfWeekStartIndexZero = false
            });
        }

        return "Invalid Cron expression";
    }

    /// <summary>
    /// Generates a sequence of DateTimes for when the schedule is triggered
    /// </summary>
    /// <param name="schedule"><see cref="Schedule"></see> object whose Cron is used to parse DateTimes</param>
    /// <param name="start">Optionally provide start time to filter generated sequence to only include DateTimes beyond a certain point. By default, DateTimeOffset.UtcNow is used.</param>
    /// <returns></returns>
    public static IEnumerable<DateTime?> NextFireTimes(this Schedule schedule, DateTimeOffset? start = null)
    {
        if (!CronExpression.IsValidExpression(schedule.CronExpression))
        {
            yield break;
        }
        
        var cron = new CronExpression(schedule.CronExpression);
        DateTimeOffset? dateTime = start ?? DateTimeOffset.UtcNow;
        while (dateTime is not null)
        {
            dateTime = cron.GetTimeAfter((DateTimeOffset)dateTime);
            if (dateTime is null)
            {
                break;
            }
            yield return dateTime.Value.LocalDateTime;
        }
    }

    /// <summary>
    /// Checks whether a schedule will trigger between the provided datetime range.
    /// </summary>
    /// <param name="schedule"><see cref="Schedule">Schedule</see> object to check</param>
    /// <param name="after">Lower bound of the time range. <see langword="null"/> if no lower bound.</param>
    /// <param name="before">Upper bound of the time range. <see langword="null"/> if no upper bound.</param>
    /// <returns><see langword="true"/> if the schedule triggers between the given range, <see langword="fals"/> if not.</returns>
    public static bool TriggersBetween(this Schedule schedule, DateTime? after, DateTime? before)
    {
        if (!CronExpression.IsValidExpression(schedule.CronExpression))
        {
            return false;
        }
        
        if (after is null && before is null)
        {
            return true;
        }
        
        var cron = new CronExpression(schedule.CronExpression);
        return (after, before) switch
        {
            ({ } a, { } b) => cron.GetTimeAfter(a) is { } dto && dto <= b,
            ({ } a, _) => cron.GetTimeAfter(a) is not null,
            (_, { } b) => cron.GetTimeAfter(DateTimeOffset.MinValue) <= b,
            _ => true
        };
    }

    public static string FormatPercentage(this decimal value, int decimalPlaces)
    {
        return decimal.Round(value, decimalPlaces) + "%";
    }

    /// <summary>
    /// Round DateTime backwards based on ticks parameter
    /// </summary>
    /// <remarks>
    /// Example usage to round to nearest minute
    /// <code>
    /// var rounded = DateTime.Now.Trim(TimeSpan.TicksPerMinute);
    /// </code>
    /// </remarks>
    /// <param name="date">DateTime to round</param>
    /// <param name="roundTicks">Number of ticks to use for rounding, e.g. TimeSpan.TicksPerMinute to round to nearest minute</param>
    /// <returns>Rounded DateTime</returns>
    public static DateTime Trim(this DateTime date, long roundTicks)
    {
        return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
    }

    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }

    public static bool EqualsIgnoreCase(this string source, string? toCheck) => toCheck switch
    {
        not null => source.Equals(toCheck, StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public static bool ContainsIgnoreCase(this string source, string? toCheck) => toCheck switch
    {
        not null => source.Contains(toCheck, StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public static Task LetAsync<T>(this T? obj, Func<T, Task> block) => obj switch
    {
        not null => block(obj),
        _ => Task.CompletedTask
    };

    public static async Task<R?> LetAsync<T, R>(this T? obj, Func<T, Task<R>> block)
        where R : class =>
        obj switch
        {
            not null => await block(obj),
            _ => null
        };

    public static TValue? GetValueOrDefault<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key)
        where TKey : class
        where TValue : class => table.TryGetValue(key, out var value) switch
        {
            true => value,
            false => null
        };

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> table, TKey key)
        where TValue : new()
    {
        if (table.TryGetValue(key, out var value))
        {
            return value;
        }

        value = new TValue();
        table[key] = value;
        return value;
    }
}