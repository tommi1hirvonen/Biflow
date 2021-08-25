using EtlManagerDataAccess.Models;
using EtlManagerUtils;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Quartz;
using System.Collections.Generic;

namespace EtlManagerUi
{
    public static partial class Utility
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

        public static async Task StopExecutionAsync(this StepExecutionAttempt attempt, string username)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", attempt.ExecutionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            var cancelCommand = new CancelCommand(attempt.StepId, username);
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }

        public static async Task StopExecutionAsync(this Execution execution, string username)
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", execution.ExecutionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            var cancelCommand = new CancelCommand(null, username);
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }

        public static (int Offset, int Width) GetGanttGraphDimensions(this StepExecutionAttempt attempt)
        {
            var allAttempts = attempt.StepExecution.Execution.StepExecutions
                .SelectMany(e => e.StepExecutionAttempts)
                .Where(e => e.StartDateTime != null);

            if (!allAttempts.Any())
                return (0, 0);

            var minTime = allAttempts.Min(e => e.StartDateTime?.LocalDateTime) ?? DateTime.Now;
            var maxTime = allAttempts.Max(e => e.EndDateTime?.LocalDateTime ?? DateTime.Now);

            var minTicks = minTime.Ticks;
            var maxTicks = maxTime.Ticks;

            if (minTicks == maxTicks)
                return (0, 0);

            var startTicks = (attempt.StartDateTime?.LocalDateTime ?? DateTime.Now).Ticks;
            var endTicks = (attempt.EndDateTime?.LocalDateTime ?? DateTime.Now).Ticks;

            var start = (double)(startTicks - minTicks) / (maxTicks - minTicks) * 100;
            var end = (double)(endTicks - minTicks) / (maxTicks - minTicks) * 100;
            var width = end - start;
            width = width < 1 ? 1 : width; // check that width is not 0
            start = start > 99 ? 99 : start; // check that start is not 100

            return ((int)Math.Round(start, 0), (int)Math.Round(width, 0));
        }

        public static decimal GetSuccessPercent(this Execution execution)
        {
            var successCount = execution.StepExecutions?.Count(step => step.StepExecutionAttempts?.Any(attempt => attempt.ExecutionStatus == StepExecutionStatus.Succeeded) ?? false) ?? 0;
            var allCount = execution.StepExecutions?.Count ?? 0;
            return allCount > 0 ? (decimal)successCount / allCount * 100 : 0;
        }

        public static int GetProgressPercent(this Execution execution)
        {
            var allCount = execution.StepExecutions?.Count ?? 0;
            var completedCount = execution.StepExecutions?.Count(step =>
                step.StepExecutionAttempts?.Any(att =>
                    att.ExecutionStatus == StepExecutionStatus.Succeeded ||
                    att.ExecutionStatus == StepExecutionStatus.Failed ||
                    att.ExecutionStatus == StepExecutionStatus.Stopped ||
                    att.ExecutionStatus == StepExecutionStatus.Skipped ||
                    att.ExecutionStatus == StepExecutionStatus.Duplicate) ?? false) ?? 0;
            return allCount > 0 ? (int)Math.Round(completedCount / (double)allCount * 100) : 0;
        }

        public static string GetScheduleSummary(this Schedule schedule)
        {
            if (schedule.CronExpression is not null && CronExpression.IsValidExpression(schedule.CronExpression))
            {
                var cron = new CronExpression(schedule.CronExpression);
                return cron.GetExpressionSummary();
            }
            else
            {
                return "Invalid Cron expression";
            }
        }

        public static DateTime GetNextFireTime(this Schedule schedule) => schedule.NextFireTimesSequence().FirstOrDefault();

        public static IEnumerable<DateTime> GetNextFireTimes(this Schedule schedule, int count) => schedule.NextFireTimesSequence().Take(count);

        private static IEnumerable<DateTime> NextFireTimesSequence(this Schedule schedule)
        {
            if (schedule.CronExpression is not null && CronExpression.IsValidExpression(schedule.CronExpression))
            {
                var cron = new CronExpression(schedule.CronExpression);
                DateTimeOffset? dateTime = DateTimeOffset.UtcNow;
                while (dateTime is not null)
                {
                    dateTime = cron.GetTimeAfter((DateTimeOffset)dateTime);
                    if (dateTime is null)
                        break;
                    else
                        yield return dateTime.Value.LocalDateTime;
                }
            }
        }

        public static string FormatPercentage(this decimal value, int decimalPlaces)
        {
            return decimal.Round(value, decimalPlaces).ToString() + "%";
        }

        public static DateTime Trim(this DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks, date.Kind);
        }

        public static string Left(this string value, int length)
        {
            if (value.Length > length)
            {
                return value.Substring(0, length);
            }

            return value;
        }

        public static DateTime ToDateTime(this long value)
        {
            return new DateTime(value);
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}
