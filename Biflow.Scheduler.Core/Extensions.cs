using Biflow.Core;
using Biflow.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Biflow.Scheduler.Core;

public static class Extensions
{
    public static void AddSchedulerServices<TExecutionJob>(this IServiceCollection services)
        where TExecutionJob : ExecutionJobBase
    {
        services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddDbContextFactory<BiflowContext>();
        services.AddExecutionBuilderFactory();
        services.AddSqlConnectionFactory();
        services.AddSingleton<ISchedulesManager, SchedulesManager<TExecutionJob>>();
    }
}
