using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Biflow.Scheduler.Core;

public static class Extensions
{
    public static void AddSchedulerServices<TExecutionJob>(this IServiceCollection services, string biflowConnectionString)
        where TExecutionJob : ExecutionJobBase
    {
        services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddDbContextFactory<BiflowContext>(options => options.UseSqlServer(biflowConnectionString));
        services.AddSingleton<ISchedulesManager, SchedulesManager<TExecutionJob>>();
    }
}
