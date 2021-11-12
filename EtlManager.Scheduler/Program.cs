using EtlManager.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.Scheduler;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging((hostContext, loggingBuilder) =>
            {
                var logger = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();
                loggingBuilder.AddSerilog(logger, dispose: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
                services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
                var connectionString = hostContext.Configuration.GetConnectionString("EtlManagerContext");
                services.AddDbContextFactory<EtlManagerContext>(options => options.UseSqlServer(connectionString));
                services.AddHostedService<Worker>();
            });
}
