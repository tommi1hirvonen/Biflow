using EtlManager.DataAccess;
using EtlManager.Executor.Core.Common;
using EtlManager.Executor.Core.ConnectionTest;
using EtlManager.Executor.Core.JobExecutor;
using EtlManager.Executor.Core.Notification;
using EtlManager.Executor.Core.Orchestrator;
using EtlManager.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtlManager.Executor.Core;

public static class Extensions
{
    public static void AddExecutorServices<TExecutorLauncher>(
        this IServiceCollection services,
        string etlManagerConnectionString,
        IConfigurationSection? baseSection = null)
        where TExecutorLauncher : class, IExecutorLauncher
    {
        services.AddDbContextFactory<EtlManagerContext>(options =>
        options.UseSqlServer(etlManagerConnectionString, o =>
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
