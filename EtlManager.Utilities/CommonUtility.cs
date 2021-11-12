using EtlManager.DataAccess.Models;
using System.IO.Pipes;

namespace EtlManager.Utilities;

public static class CommonUtility
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

    public static byte[] ReadMessage(PipeStream pipe)
    {
        byte[] buffer = new byte[1024];
        using var ms = new MemoryStream();
        do
        {
            var readBytes = pipe.Read(buffer, 0, buffer.Length);
            ms.Write(buffer, 0, readBytes);
        }
        while (!pipe.IsMessageComplete);

        return ms.ToArray();
    }
}
