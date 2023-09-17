using Biflow.Core;
using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.StepExecutor;
using Biflow.Executor.Core.WebExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core;

public static class Extensions
{
    public static void AddExecutorServices(this IServiceCollection services, IConfiguration executorConfiguration) =>
        AddExecutorServices<ExecutorLauncher>(services, executorConfiguration);

    public static void AddExecutorServices<TExecutorLauncher>(
        this IServiceCollection services,
        IConfiguration executorConfiguration)
        where TExecutorLauncher : class, IExecutorLauncher
    {
        services.AddSqlConnectionFactory();
        services.AddDbContextFactory<ExecutorDbContext>();
        services.AddDbContextFactory<BiflowContext>();
        services.AddExecutionBuilderFactory();
        services.AddHttpClient();
        services.AddHttpClient("notimeout", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddOptions<ExecutionOptions>()
            .Bind(executorConfiguration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<EmailOptions>(executorConfiguration);
        services.AddSingleton<INotificationService, EmailService>();
        services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();
        services.AddSingleton<IGlobalOrchestrator, GlobalOrchestrator>();
        services.AddSingleton<IJobOrchestratorFactory, JobOrchestratorFactory>();
        services.AddSingleton<IEmailTest, EmailTest>();
        services.AddSingleton<IConnectionTest, ConnectionTest.ConnectionTest>();
        services.AddSingleton<IJobExecutorFactory, JobExecutorFactory>();
        services.AddSingleton<IExecutionManager, ExecutionManager>();
        services.AddTransient<IExecutorLauncher, TExecutorLauncher>();
    }

    /// <summary>
    /// Replace sections of a string based on multiple rules.
    /// The method makes sure not to replace the same section twice.
    /// </summary>
    /// <param name="input">The string to which the replacement rules are applied</param>
    /// <param name="replacementRules">Replacement rules where the key is the substring to search for and the value is the replacement.</param>
    /// <returns></returns>
    internal static string Replace(this string input, IDictionary<string, string?> replacementRules)
    {
        var matches = replacementRules.Where(rule => input.Contains(rule.Key));
        if (!matches.Any())
        {
            return input;
        }

        var match = matches.First();
        int startIndex = input.IndexOf(match.Key);
        int endIndex = startIndex + match.Key.Length;

        var before = input[..startIndex].Replace(replacementRules);
        var replaced = match.Value;
        var after = input[endIndex..].Replace(replacementRules);

        return before + replaced + after;
    }

    /// <summary>
    /// Maps step execution parameters to a Dictionary
    /// </summary>
    /// <param name="parameters">Step execution parameters to map</param>
    /// <returns>Dictionary where the parameter name is the key and the parameter value is the value</returns>
    internal static Dictionary<string, string?> ToStringDictionary(this IEnumerable<StepExecutionParameterBase> parameters)
    {
        return parameters.Select(p => p.ParameterValue switch
        {
            DateTime dt => (Name: p.ParameterName, Value: dt.ToString("o")),
            _ => (Name: p.ParameterName, Value: p.ParameterValue?.ToString())
        })
        .ToDictionary(key => key.Name, value => value.Value);
    }

    internal static IEnumerable<T> SelectNotNull<T, U>(this IEnumerable<U> source, Func<U, T?> selector)
        where T : class
    {
        return source.Select(selector).Where(t => t is not null).Cast<T>();
    }
}
