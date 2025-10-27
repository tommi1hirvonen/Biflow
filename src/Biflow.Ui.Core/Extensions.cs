using System.Linq.Expressions;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using CronExpressionDescriptor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Runtime.CompilerServices;
using System.Transactions;
using Biflow.Ui.Core.Validation;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StartEnd = (System.DateTimeOffset? Start, System.DateTimeOffset? End);

namespace Biflow.Ui.Core;

public static class Extensions
{
    /// <summary>
    /// Adds services that provide core functionality in the UI application
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">Top level configuration object</param>
    /// <param name="authenticationConfiguration">key of the user authentication method configuration</param>
    /// <typeparam name="TUserService">type implementing <see cref="IUserService"/> registered as a scoped service</typeparam>
    /// <returns>The IServiceCollection passed as parameter</returns>
    /// <exception cref="ArgumentException">Thrown if an incorrect configuration is detected</exception>
    public static IServiceCollection AddUiCoreServices<TUserService>(
        this IServiceCollection services,
        IConfiguration configuration,
        string authenticationConfiguration = "Authentication")
        where TUserService : class, IUserService
    {
        // Add the UserService and AppDbContext factory as scoped.
        // The current user is captured and stored in UserService,
        // which in turn is used in AppDbContext to filter data in global query filters
        // based on the user's access permissions.
        services.AddScoped<IUserService, TUserService>();
        services.AddDbContextFactory<AppDbContext>(lifetime: ServiceLifetime.Scoped);

        // Add additional DbContext factories with singleton lifetime.
        // These are used in background services where the user session is not relevant.
        services.AddDbContextFactory<ServiceDbContext>(lifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<RevertDbContext>(lifetime: ServiceLifetime.Singleton);

        services.AddExecutionBuilderFactory<AppDbContext>(ServiceLifetime.Scoped);
        
        services.AddHttpClient();

        services.AddSingleton<ITokenService, TokenService<ServiceDbContext>>();
        
        var authentication = configuration.GetValue<string>(authenticationConfiguration);
        var authMethod = authentication switch
        {
            "BuiltIn" => AuthenticationMethod.BuiltIn,
            "Windows" => AuthenticationMethod.Windows,
            "AzureAd" => AuthenticationMethod.AzureAd,
            "Ldap" => AuthenticationMethod.Ldap,
            _ => throw new ArgumentException($"Invalid Authentication setting: {authentication}"),
        };
        services.AddSingleton(new AuthenticationMethodResolver(authMethod));

        var executorType = configuration.GetSection("Executor").GetValue<string>("Type");
        ExecutorMode executorMode;
        switch (executorType)
        {
            case "WebApp":
                services.AddSingleton<IExecutorService, WebAppExecutorService>();
                executorMode = ExecutorMode.WebApp;
                break;
            case "SelfHosted":
                services.AddExecutorServices(configuration.GetSection("Executor").GetSection("SelfHosted"));
                services.AddSingleton<IExecutorService, SelfHostedExecutorService>();
                executorMode = ExecutorMode.SelfHosted;
                break;
            default:
                throw new ArgumentException($"Error registering executor service. Incorrect executor type: {executorType}. Check appsettings.json.");
        }

        var schedulerType = configuration.GetSection("Scheduler").GetValue<string>("Type");
        SchedulerMode schedulerMode;
        switch (schedulerType)
        {
            case "WebApp":
                services.AddSingleton<ISchedulerService, WebAppSchedulerService>();
                schedulerMode = SchedulerMode.WebApp;
                break;
            case "SelfHosted":
                services.AddSchedulerServices<ExecutionJob>();
                services.AddSingleton<ISchedulerService, SelfHostedSchedulerService>();
                schedulerMode = SchedulerMode.SelfHosted;
                break;
            default:
                throw new ArgumentException($"Error registering scheduler service. Incorrect scheduler type: {schedulerType}. Check appsettings.json.");
        }

        services.AddSingleton(new ExecutorModeResolver(executorMode));
        services.AddSingleton(new SchedulerModeResolver(schedulerMode));
        services.AddSingleton<ProxyClientFactory>();
        services.AddScoped<EnvironmentSnapshotBuilder>();
        services.AddDuplicatorServices();

        // Add the mediator dispatcher as a scoped service.
        // This allows the use of other scoped services (e.g. AppDbContext factory) in request handlers.
        services.AddScoped<IMediator, Mediator>();

        // Add request handlers
        services.AddRequestHandlers<Mediator>();
        
        return services;
    }

    /// <summary>
    /// Adds validation services used for more complex validation rules for some entities
    /// </summary>
    /// <returns>The IServiceCollection passed as parameter</returns>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddTransient<StepParametersValidator>();
        services.AddTransient<StepValidator>();
        services.AddTransient<JobValidator>();
        services.AddTransient<DataTableValidator>();
        services.AddTransient<ScdTableValidator>();
        return services;
    }

    /// <summary>
    /// Extensions to help instruct EF Core ChangeTracker which collection navigation items have changed
    /// when doing a disconnected update.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <param name="context">DbContext instance whose ChangeTracker is used</param>
    /// <param name="currentItems">Current (old) items</param>
    /// <param name="newItems">New items</param>
    /// <param name="keyFunc">Delegate to get the key from item</param>
    /// <param name="updateMatchingItemValues">call <see cref="PropertyValues.SetValues(object)"/> on matching items</param>
    public static void MergeCollections<T, TKey>(
        this DbContext context,
        ICollection<T> currentItems,
        ICollection<T> newItems,
        Func<T, TKey> keyFunc,
        bool updateMatchingItemValues = true)
        where T : class
    {
        List<T> toRemove = [];
        foreach (var item in currentItems)
        {
            var currentKey = keyFunc(item);
            ArgumentNullException.ThrowIfNull(currentKey);
            var found = newItems.FirstOrDefault(x => currentKey.Equals(keyFunc(x)));
            if (found is null)
            {
                toRemove.Add(item);
            }
            else if (updateMatchingItemValues && !ReferenceEquals(found, item))
            {
                context.Entry(item).CurrentValues.SetValues(found);
            }
        }

        foreach (var item in toRemove)
        {
            currentItems.Remove(item);
            // The following call can be activated if the removed item
            // should be completely deleted from the database.
            // context.Set<T>().Remove(item);
        }

        List<T> toAdd = [];
        foreach (var newItem in newItems)
        {
            var newKey = keyFunc(newItem);
            ArgumentNullException.ThrowIfNull(newKey);
            var found = currentItems.FirstOrDefault(x => newKey.Equals(keyFunc(x)));
            if (found is null)
            {
                toAdd.Add(newItem);
            }
        }

        foreach (var item in toAdd)
        {
            currentItems.Add(item);
        }
    }

    /// <summary>
    /// Executes a query asynchronously and retrieves the results as a list,
    /// using a transaction scope with read-uncommitted isolation level to avoid locking.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <typeparam name="T">The type of elements in the query result.</typeparam>
    /// <returns>A task that represents the asynchronous operation, containing the list of query results.</returns>
    public static async Task<List<T>> ToListWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.ToListAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Executes a query asynchronously and retrieves the results as an array,
    /// using a transaction scope with read-uncommitted isolation level to avoid locking.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <typeparam name="T">The type of elements in the query result.</typeparam>
    /// <returns>A task that represents the asynchronous operation, containing the array of query results.</returns>
    public static async Task<T[]> ToArrayWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.ToArrayAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Retrieves the first element of a sequence, or a default value, without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable sequence to retrieve the element from.</param>
    /// <param name="cancellationToken">Optional cancellation token for managing task cancellation.</param>
    /// <typeparam name="T">The type of the elements in the queryable sequence.</typeparam>
    /// <returns>The first element in that meets the specified condition, or the default value of type T if no such element is found.</returns>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.FirstOrDefaultAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a specified predicate or a default value,
    /// without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable source to operate on.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A token to monitor for operation cancellation.</param>
    /// <typeparam name="T">The type of elements in the query.</typeparam>
    /// <returns>The first element which satisfies the predicate, or default if no such element is found.</returns>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> query,
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.FirstOrDefaultAsync(predicate, cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Determines whether any elements exist in the query without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable data source to evaluate.</param>
    /// /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A token to monitor for task cancellation.</param>
    /// <typeparam name="T">Type of the elements in the query.</typeparam>
    /// <returns>True if the query contains any elements; otherwise, false.</returns>
    public static async Task<bool> AnyWithNoLockAsync<T>(this IQueryable<T> query,
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.AnyAsync(predicate, cancellationToken);
        scope.Complete();
        return result;
    }
    
    private static TransactionScope CreateReadUncommittedTransaction() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted },
        TransactionScopeAsyncFlowOption.Enabled);

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

    public static async Task<TR?> LetAsync<T, TR>(this T? obj, Func<T, Task<TR>> block)
        where TR : class =>
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
