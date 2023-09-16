using Biflow.Core;
using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using CronExpressionDescriptor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Quartz;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using StartEnd = System.ValueTuple<System.DateTimeOffset?, System.DateTimeOffset?>;

namespace Biflow.Ui.Core;

public static partial class Extensions
{

    /// <summary>
    /// If the UI application uses the self-hosted scheduler service to launch executions, this method should be called right before app.Run().
    /// Loads all schedules from the database to the in-memory scheduler service.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async Task ReadAllSchedulesAsync(this WebApplication app)
    {
        // Read all schedules into the schedules manager.
        using var scope = app.Services.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
        await scheduler.SynchronizeAsync();
    }

    /// <summary>
    /// Adds authentication services based on settings defined in configuration
    /// </summary>
    /// <param name="configuration">Top level configuration object</param>
    /// <returns>The IServiceCollection passed as parameter</returns>
    /// <exception cref="ArgumentException">Thrown if an incorrect configuration is detected</exception>
    public static IServiceCollection AddUiCoreAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authentication = configuration.GetValue<string>("Authentication");
        AuthenticationMethod method;
        if (authentication == "BuiltIn")
        {
            services.AddSingleton<IAuthHandler, BuiltInAuthHandler>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            method = AuthenticationMethod.BuiltIn;
        }
        else if (authentication == "Windows")
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
            services.AddAuthorization(options =>
            {
                var connectionString = configuration.GetConnectionString("BiflowContext");
                ArgumentNullException.ThrowIfNull(connectionString);
                options.FallbackPolicy = new AuthorizationPolicyBuilder().AddRequirements(new UserExistsRequirement(connectionString)).Build();
            });
            services.AddSingleton<IAuthorizationHandler, WindowsAuthorizationHandler>();
            services.AddSingleton<IClaimsTransformation, ClaimsTransformer>();
            method = AuthenticationMethod.Windows;
        }
        else if (authentication == "AzureAd")
        {
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));
            services.AddControllersWithViews().AddMicrosoftIdentityUI();
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });
            services.AddSingleton<IClaimsTransformation, ClaimsTransformer>();
            method = AuthenticationMethod.AzureAd;
        }
        else if (authentication == "Ldap")
        {
            services.AddSingleton<IAuthHandler, LdapAuthHandler>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            method = AuthenticationMethod.Ldap;
        }
        else
        {
            throw new ArgumentException($"Invalid Authentication setting: {authentication}");
        }
        services.AddSingleton(new AuthenticationMethodResolver(method));
        return services;
    }

    /// <summary>
    /// Adds services that provide core functionality in the UI application
    /// </summary>
    /// <param name="configuration">Top level configuration object</param>
    /// <returns>The IServiceCollection passed as parameter</returns>
    /// <exception cref="ArgumentException">Thrown if an incorrect configuration is detected</exception>
    public static IServiceCollection AddUiCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlConnectionFactory();
        services.AddDbContextFactory<BiflowContext>();
        services.AddExecutionBuilderFactory();
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
            services.AddExecutorServices<ExecutorLauncher>(configuration.GetSection("Executor").GetSection("SelfHosted"));
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
            services.AddSchedulerServices<ExecutionJob>();
            services.AddSingleton<ISchedulerService, SelfHostedSchedulerService>();
        }
        else
        {
            throw new ArgumentException($"Error registering scheduler service. Incorrect scheduler type: {schedulerType}. Check appsettings.json.");
        }

        services.AddSingleton<DbHelperService>();
        services.AddSingleton<SqlServerHelperService>();
        services.AddSingleton<SubscriptionsHelperService>();
        services.AddDuplicatorServices();

        return services;
    }

    /// <summary>
    /// Adds validation services used for more complex validation rules for some entities
    /// </summary>
    /// <returns>The IServiceCollection passed as parameter</returns>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddScoped<StepParametersValidator>();
        services.AddScoped<StepValidator>();
        services.AddScoped<JobValidator>();
        services.AddScoped<DataTableValidator>();
        return services;
    }

    /// <summary>
    /// Try to identify and parse a SQL stored procedure from a SQL statement
    /// </summary>
    /// <remarks>For example, SQL statement <c>exec [dbo].[MyProc]</c> would return a schema of dbo and procedure name MyProc</remarks>
    /// <returns>Tuple of strings if the stored procedure was parsed successfully, null if not. The schema is null if the SQL statement did not include a schema.</returns>
    public static (string? Schema, string ProcedureName)? ParseStoredProcedureFromSqlStatement(this string sqlStatement)
    {
        // Can handle white space inside object names
        var regex1 = ProcedureWithSchemaWithBracketsRegex();
        var match1 = regex1.Match(sqlStatement);
        if (match1.Success)
        {
            var schema = match1.Groups[1].Value[1..^1]; // skip first and last character
            var proc = match1.Groups[2].Value[1..^1];
            return (schema, proc);
        }

        // No square brackets => no whitespace in object names
        var regex2 = ProcedureWithSchemaWithoutBracketsRegex();
        var match2 = regex2.Match(sqlStatement);
        if (match2.Success)
        {
            var schema = match2.Groups[1].Value;
            var proc = match2.Groups[2].Value;
            return (schema, proc);
        }

        // Can handle white space inside object names
        var regex3 = ProcedureWithoutSchemaWithBracketsRegex();
        var match3 = regex3.Match(sqlStatement);
        if (match3.Success)
        {
            var proc = match3.Groups[1].Value[1..^1]; // skip first and last character
            return (null, proc);
        }

        // No square brackets => no whitespace in object names
        var regex4 = ProcedureWithoutSchemaWithoutBracketsRegex();
        var match4 = regex4.Match(sqlStatement);
        if (match4.Success)
        {
            var proc = match4.Groups[1].Value;
            return (null, proc);
        }

        return null;
    }

    public static (double Offset, double Width) GetGanttGraphDimensions(this StepExecutionAttempt attempt)
    {
        var allAttempts = attempt.StepExecution.Execution.StepExecutions
            .SelectMany(e => e.StepExecutionAttempts)
            .Where(e => e.StartDateTime != null);

        return attempt.GetGanttGraphDimensions(allAttempts);
    }

    /// <summary>
    /// Calculate Gantt graph dimensions for an execution attempt. The start and end time are compared to the list of all attempts provider as an argument.
    /// The method assumes constant width of 100 for the Gantt graph.
    /// </summary>
    /// <param name="allAttempts">List of all execution attempts shown on the Gantt graph</param>
    /// <returns>Offset (between 0 and 99) and width (between 1 and 100) of the element in the Gantt graph</returns>
    public static (double Offset, double Width) GetGanttGraphDimensions(this StepExecutionAttempt attempt, IEnumerable<StepExecutionAttempt> allAttempts)
        => (attempt.StartDateTime, attempt.EndDateTime).GetGanttGraphDimensions(allAttempts.Select(a => (a.StartDateTime, a.EndDateTime)));

    public static (double Offset, double Width) GetGanttGraphDimensions(this Execution execution, IEnumerable<Execution> allExecutions)
        => (execution.StartDateTime, execution.EndDateTime).GetGanttGraphDimensions(allExecutions.Select(e => (e.StartDateTime, e.EndDateTime)));

    /// <summary>
    /// Calculate Gantt graph dimensions for a tuple of DateTimeOffsets (start and end time). The start and end time are compared to the list of all tuples provided as an argument.
    /// The method assumes constant width of 100 for the Gantt graph.
    /// </summary>
    /// <param name="allExecutions">List of all executions (start and end times) shown on the Gantt graph</param>
    /// <returns>Offset (between 0 and 99) and width (between 1 and 100) of the element in the Gantt graph</returns>
    public static (double Offset, double Width) GetGanttGraphDimensions(this StartEnd execution, IEnumerable<StartEnd> allExecutions)
    {
        if (!allExecutions.Any())
            return (0, 0);

        var minTime = allExecutions.Min(e => e.Item1?.LocalDateTime) ?? DateTime.Now;
        var maxTime = allExecutions.Max(e => e.Item2?.LocalDateTime ?? DateTime.Now);

        var minTicks = minTime.Ticks;
        var maxTicks = maxTime.Ticks;

        if (minTicks == maxTicks)
            return (0, 0);

        var startTicks = (execution.Item1?.LocalDateTime ?? DateTime.Now).Ticks;
        var endTicks = (execution.Item2?.LocalDateTime ?? DateTime.Now).Ticks;

        var start = (double)(startTicks - minTicks) / (maxTicks - minTicks) * 100;
        var end = (double)(endTicks - minTicks) / (maxTicks - minTicks) * 100;
        var width = end - start;
        width = width < 1 ? 1 : width; // check that width is not 0
        start = start > 99 ? 99 : start; // check that start is not 100

        return (start, width);
    }

    /// <summary>
    /// Calculate progress percentage based on the <see cref="Execution.StepExecutions"/> and <see cref="StepExecution.StepExecutionAttempts"/> list of steps and their statuses
    /// </summary>
    /// <returns>Success percentage between 0 and 100</returns>
    public static decimal GetSuccessPercent(this Execution execution)
    {
        var successCount = execution.StepExecutions
            ?.Count(step =>
                step.StepExecutionAttempts?.Any(attempt =>
                    attempt.ExecutionStatus == StepExecutionStatus.Succeeded || attempt.ExecutionStatus == StepExecutionStatus.Warning) ?? false) ?? 0;
        var allCount = execution.StepExecutions?.Count ?? 0;
        return allCount > 0 ? (decimal)successCount / allCount * 100 : 0;
    }

    /// <summary>
    /// Calculate progress percentage based on the <see cref="Execution.StepExecutions"/> and <see cref="StepExecution.StepExecutionAttempts"/> list of steps and their statuses
    /// </summary>
    /// <returns>Progress percentage between 0 and 100 rounded to the nearest integer</returns>
    public static int GetProgressPercent(this Execution execution)
    {
        var allCount = execution.StepExecutions?.Count ?? 0;
        var completedCount = execution.StepExecutions?.Count(step =>
            step.StepExecutionAttempts?.Any(att =>
                att.ExecutionStatus == StepExecutionStatus.Succeeded ||
                att.ExecutionStatus == StepExecutionStatus.Warning ||
                att.ExecutionStatus == StepExecutionStatus.Failed ||
                att.ExecutionStatus == StepExecutionStatus.Stopped ||
                att.ExecutionStatus == StepExecutionStatus.Skipped ||
                att.ExecutionStatus == StepExecutionStatus.Duplicate) ?? false) ?? 0;
        return allCount > 0 ? (int)Math.Round(completedCount / (double)allCount * 100) : 0;
    }

    /// <summary>
    /// Get a string describing the schedule's underlying Cron expression
    /// </summary>
    /// <returns>Descriptive text if the Cron expression is valid. Otherwise an error message string is returned.</returns>
    public static string GetScheduleDescription(this Schedule schedule) => GetCronExpressionDescription(schedule.CronExpression);

    /// <summary>
    /// Get a string describing a Cron expression
    /// </summary>
    /// <param name="expression">String to read as Cron expression</param>
    /// <returns>Descriptive text if the Cron expression is valid. Otherwise an error message string is returned.</returns>
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
        else
        {
            return "Invalid Cron expression";
        }
    }

    /// <summary>
    /// Generates a sequence of DateTimes for when the schedule is triggered
    /// </summary>
    /// <param name="schedule"><see cref="">Schedule</see> object whose Cron is used to parse DateTimes</param>
    /// <param name="start">Optionally provide start time to filter generated sequence to only include DateTimes beyond a certain point. By default DateTimeOffset.UtcNow is used.</param>
    /// <returns></returns>
    public static IEnumerable<DateTime?> NextFireTimes(this Schedule schedule, DateTimeOffset? start = null)
    {
        if (schedule.CronExpression is not null && CronExpression.IsValidExpression(schedule.CronExpression))
        {
            var cron = new CronExpression(schedule.CronExpression);
            DateTimeOffset? dateTime = start ?? DateTimeOffset.UtcNow;
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

    public static async Task<R?> LetAsync<T, R>(this T? obj, Func<T, Task<R>> block) where R : class => obj switch
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

    // Using the GeneratedRegex attributes we can create the regex already at compile time.

    // Can handle white space inside object names
    [GeneratedRegex("EXEC(?:UTE)?[\\s*](\\[.*\\]).(\\[.*\\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithSchemaWithBracketsRegex();

    // No square brackets => no whitespace in object names
    [GeneratedRegex("EXEC(?:UTE)?[\\s*](\\S*)\\.(\\S*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithSchemaWithoutBracketsRegex();

    // Can handle white space inside object names
    [GeneratedRegex("EXEC(?:UTE)?[\\s*](\\[.*\\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithoutSchemaWithBracketsRegex();

    // No square brackets => no whitespace in object names
    [GeneratedRegex("EXEC(?:UTE)?[\\s*](\\S*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithoutSchemaWithoutBracketsRegex();
}
