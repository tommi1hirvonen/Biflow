using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core;

public static class Extensions
{
    public static void AddExecutorServices<TExecutorLauncher>(
        this IServiceCollection services,
        string biflowConnectionString,
        IConfigurationSection? baseSection = null)
        where TExecutorLauncher : class, IExecutorLauncher
    {
        
        services.AddDbContextFactory<BiflowContext>((services, options) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var sensitiveDataLogging = (baseSection ?? configuration).GetValue<bool>("SensitiveDataLogging");
            options.EnableSensitiveDataLogging(sensitiveDataLogging)
            .UseSqlServer(biflowConnectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));    
        });
        services.AddHttpClient();
        services.AddHttpClient("notimeout", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IExecutionConfiguration, ExecutionConfiguration>(services =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            return new ExecutionConfiguration(configuration, baseSection);
        });
        services.AddSingleton<IEmailConfiguration, EmailConfiguration>(services =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            return new EmailConfiguration(configuration, baseSection);
        });
        services.AddSingleton<INotificationService, EmailService>();
        services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();
        services.AddSingleton<IOrchestratorFactory, OrchestratorFactory>();
        services.AddSingleton<IGlobalOrchestrator, GlobalOrchestrator>();
        services.AddSingleton<IEmailTest, EmailTest>();
        services.AddSingleton<IConnectionTest, ConnectionTest.ConnectionTest>();
        services.AddSingleton<IJobExecutorFactory, JobExecutorFactory>();
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
