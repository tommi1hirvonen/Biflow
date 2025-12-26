using Biflow.Executor.Core.ExecutionValidation;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.JobOrchestrator;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Notification.Options;
using Biflow.Executor.Core.Orchestrator;
using Biflow.Executor.Core.ServiceHealth;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biflow.Executor.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddExecutorServices(this IServiceCollection services,
        IConfiguration executorConfiguration)
    {
        services.AddCoreServices();
        services.AddDbContextFactory<ExecutorDbContext>();
        services.AddExecutionBuilderFactory<ExecutorDbContext>();
        services.AddHttpClient();
        services.AddHttpClient("notimeout", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddMemoryCache();
        services.AddSingleton(typeof(ITokenService), typeof(TokenService<ExecutorDbContext>));
        services.AddOptions<ExecutionOptions>()
            .Bind(executorConfiguration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IExecutionValidator, CircularJobsValidator>();
        services.AddSingleton<IExecutionValidator, CircularStepsValidator>();
        services.AddSingleton<IExecutionValidator, HybridModeValidator>();

        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.JobExecutorHealthService);
        services.AddKeyedSingleton<HealthService>(ExecutorServiceKeys.NotificationHealthService);
        services.AddSingleton<ISubscriptionsProviderFactory, SubscriptionsProviderFactory>();
        services.AddSingleton<ISubscribersResolver, SubscribersResolver>();
        services.AddSingleton<INotificationMessageService, NotificationMessageService>();

        // Configure email options and services based on which section was configured in appsettings.
        var emailSection = executorConfiguration.GetSection(EmailOptions.Section);
        var smtpSection = emailSection.GetSection(SmtpOptions.Section);
        var azureEmailSection = emailSection.GetSection(AzureEmailOptions.Section);
        var graphEmailSection = emailSection.GetSection(GraphEmailOptions.Section);
        if (smtpSection.Exists())
        {
            services.Configure<SmtpOptions>(smtpSection);
            services.AddSingleton<IMessageDispatcher, SmtpDispatcher>();
            services.AddSingleton<IEmailTest, SmtpTest>();
        }
        else if (azureEmailSection.Exists())
        {
            services.Configure<AzureEmailOptions>(azureEmailSection);
            services.AddSingleton<IMessageDispatcher, AzureEmailDispatcher>();
            services.AddSingleton<IEmailTest, AzureEmailTest>();
        }
        else if (graphEmailSection.Exists())
        {
            services.Configure<GraphEmailOptions>(graphEmailSection);
            services.AddSingleton<IMessageDispatcher, GraphEmailDispatcher>();
            services.AddSingleton<IEmailTest, GraphEmailTest>();
        }

        services.AddSingleton<FabricItemCache>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IStepOrchestrator, StepOrchestrator>();
        services.AddSingleton<IStepExecutorProvider, StepExecutorProvider>();
        services.AddSingleton<IGlobalOrchestrator, GlobalOrchestrator>();
        services.AddSingleton<IJobOrchestratorFactory, JobOrchestratorFactory>();
        services.AddSingleton<IJobExecutorFactory, JobExecutorFactory>();
        services.AddSingleton<IExecutionManager, ExecutionManager>();
        services.AddHostedService(s => s.GetRequiredService<IExecutionManager>());
        // Timeout for hosted services (e.g., ExecutionManager) to shut down gracefully when StopAsync() is called.
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));
        
        services.AddHealthChecks()
            .AddCheck<JobExecutorHealthCheck>("job_executor", tags: ["executor"])
            .AddCheck<NotificationHealthCheck>("notification_service", tags: ["executor"]);

        return services;
    }
}