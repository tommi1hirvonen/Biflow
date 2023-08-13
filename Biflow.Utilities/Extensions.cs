using Biflow.DataAccess.Models;

namespace Biflow.Utilities;

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
}
