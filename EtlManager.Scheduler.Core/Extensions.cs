using EtlManager.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace EtlManager.Scheduler.Core;

public static class Extensions
{
    public static void AddSchedulerServices<TExecutionJob>(this IServiceCollection services, string etlManagerConnectionString)
        where TExecutionJob : ExecutionJobBase
    {
        services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddDbContextFactory<EtlManagerContext>(options => options.UseSqlServer(etlManagerConnectionString));
        services.AddSingleton<ISchedulesManager, SchedulesManager<TExecutionJob>>();
    }
}
