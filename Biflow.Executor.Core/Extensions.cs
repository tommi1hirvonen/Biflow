using Biflow.DataAccess;
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
        services.AddDbContextFactory<BiflowContext>(options =>
        options.UseSqlServer(biflowConnectionString, o =>
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
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
        services.AddSingleton<IEmailTest, EmailTest>();
        services.AddSingleton<IConnectionTest, ConnectionTest.ConnectionTest>();
        services.AddTransient<IJobExecutor, JobExecutor.JobExecutor>();
        services.AddTransient<IExecutorLauncher, TExecutorLauncher>();
    }
}
