using CronExpressionDescriptor;
using Biflow.DataAccess.Models;
using Quartz;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Biflow.Executor.Core;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using Microsoft.AspNetCore.Builder;

namespace Biflow.Ui.Core;

public static partial class Utility
{
    public static async Task ReadAllSchedulesAsync(this WebApplication app)
    {
        // Read all schedules into the schedules manager.
        using var scope = app.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
        await scheduler.SynchronizeAsync();
    }

    public static IServiceCollection AddUiCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BiflowContext");
        services.AddDbContextFactory<BiflowContext>(options =>
        {
            options.UseSqlServer(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            //options.EnableSensitiveDataLogging();
        });

        services.AddHttpClient();
        services.AddHttpClient("DefaultCredentials")
            // Passes Windows credentials in on-premise installations to the scheduler API.
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

        services.AddSingleton<ITokenService, TokenService>();

        var executorType = configuration.GetSection("Executor").GetValue<string>("Type");
        if (executorType == "ConsoleApp")
        {
            services.AddSingleton<IExecutorService, ConsoleAppExecutorService>();
        }
        else if (executorType == "WebApp")
        {
            services.AddSingleton<IExecutorService, WebAppExecutorService>();
        }
        else if (executorType == "SelfHosted")
        {
            services.AddExecutorServices<ExecutorLauncher>(connectionString, configuration.GetSection("Executor").GetSection("SelfHosted"));
            services.AddSingleton<ExecutionManager>();
            services.AddSingleton<IExecutorService, SelfHostedExecutorService>();
        }
        else
        {
            throw new ArgumentException($"Error registering executor service. Incorrect executor type: {executorType}. Check appsettings.json.");
        }

        var schedulerType = configuration.GetSection("Scheduler").GetValue<string>("Type");
        if (schedulerType == "WebApp")
        {
            services.AddSingleton<ISchedulerService, WebAppSchedulerService>();
        }
        else if (schedulerType == "SelfHosted")
        {
            services.AddSchedulerServices<ExecutionJob>(connectionString);
            services.AddSingleton<ISchedulerService, SelfHostedSchedulerService>();
        }
        else
        {
            throw new ArgumentException($"Error registering scheduler service. Incorrect scheduler type: {schedulerType}. Check appsettings.json.");
        }

        services.AddSingleton<DbHelperService>();
        services.AddSingleton<SqlServerHelperService>();
        services.AddSingleton<MarkupHelperService>();
        services.AddSingleton<SubscriptionsHelperService>();

        return services;
    }

    public static (string? Schema, string ProcedureName)? ParseStoredProcedureFromSqlStatement(this string sqlStatement)
    {
        // Can handle white space inside object names
        var regex1 = new Regex(@"EXEC(?:UTE)?[\s*](\[.*\]).(\[.*\])", RegexOptions.IgnoreCase);
        var match1 = regex1.Match(sqlStatement);
        if (match1.Success)
        {
            var schema = match1.Groups[1].Value[1..^1]; // skip first and last character
            var proc = match1.Groups[2].Value[1..^1];
            return (schema, proc);
        }

        // No square brackets => no whitespace in object names
        var regex2 = new Regex(@"EXEC(?:UTE)?[\s*](\S*)\.(\S*)", RegexOptions.IgnoreCase);
        var match2 = regex2.Match(sqlStatement);
        if (match2.Success)
        {
            var schema = match2.Groups[1].Value;
            var proc = match2.Groups[2].Value;
            return (schema, proc);
        }

        // Can handle white space inside object names
        var regex3 = new Regex(@"EXEC(?:UTE)?[\s*](\[.*\])", RegexOptions.IgnoreCase);
        var match3 = regex3.Match(sqlStatement);
        if (match3.Success)
        {
            var proc = match3.Groups[1].Value[1..^1]; // skip first and last character
            return (null, proc);
        }

        // No square brackets => no whitespace in object names
        var regex4 = new Regex(@"EXEC(?:UTE)?[\s*](\S*)", RegexOptions.IgnoreCase);
        var match4 = regex4.Match(sqlStatement);
        if (match4.Success)
        {
            var proc = match4.Groups[1].Value;
            return (null, proc);
        }

        return null;
    }

    public static (int Offset, int Width) GetGanttGraphDimensions(this StepExecutionAttempt attempt)
    {
        var allAttempts = attempt.StepExecution.Execution.StepExecutions
            .SelectMany(e => e.StepExecutionAttempts)
            .Where(e => e.StartDateTime != null);

        return attempt.GetGanttGraphDimensions(allAttempts);
    }

    public static (int Offset, int Width) GetGanttGraphDimensions(this StepExecutionAttempt attempt, IEnumerable<StepExecutionAttempt> allAttempts)
    {
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

    public static (int Offset, int Width) GetGanttGraphDimensions(this Execution execution, IEnumerable<Execution> allExecutions)
    {
        if (!allExecutions.Any())
            return (0, 0);

        var minTime = allExecutions.Min(e => e.StartDateTime?.LocalDateTime) ?? DateTime.Now;
        var maxTime = allExecutions.Max(e => e.EndDateTime?.LocalDateTime ?? DateTime.Now);

        var minTicks = minTime.Ticks;
        var maxTicks = maxTime.Ticks;

        if (minTicks == maxTicks)
            return (0, 0);

        var startTicks = (execution.StartDateTime?.LocalDateTime ?? DateTime.Now).Ticks;
        var endTicks = (execution.EndDateTime?.LocalDateTime ?? DateTime.Now).Ticks;

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

    public static string GetScheduleDescription(this Schedule schedule)
    {
        if (schedule.CronExpression is not null && CronExpression.IsValidExpression(schedule.CronExpression))
        {
            return ExpressionDescriptor.GetDescription(schedule.CronExpression, new Options
            {
                ThrowExceptionOnParseError = false,
                Use24HourTimeFormat = true,
                Locale = "en"
            }); ;
        }
        else
        {
            return "Invalid Cron expression";
        }
    }

    public static DateTime? GetNextFireTime(this Schedule schedule) => schedule.NextFireTimesSequence().FirstOrDefault();

    public static IEnumerable<DateTime?> GetNextFireTimes(this Schedule schedule, int count) => schedule.NextFireTimesSequence().Take(count);

    private static IEnumerable<DateTime?> NextFireTimesSequence(this Schedule schedule)
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

    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }

    public static bool ContainsIgnoreCase(this string source, string toCheck)
    {
        return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
    }

}
