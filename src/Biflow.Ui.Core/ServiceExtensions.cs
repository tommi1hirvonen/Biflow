using Biflow.Core;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Biflow.Ui.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Ui.Core;

public static class ServiceExtensions
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
        services.AddCoreServices();
        
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
}