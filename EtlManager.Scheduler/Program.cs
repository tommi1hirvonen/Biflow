using EtlManager.DataAccess;
using EtlManager.Scheduler;
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

var builder = Host.CreateDefaultBuilder(args);

builder.UseWindowsService();

builder.ConfigureLogging((hostContext, loggingBuilder) =>
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();
    loggingBuilder.AddSerilog(logger, dispose: true);
});

builder.ConfigureServices((hostContext, services) =>
{
    services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
    services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    var connectionString = hostContext.Configuration.GetConnectionString("EtlManagerContext");
    services.AddDbContextFactory<EtlManagerContext>(options => options.UseSqlServer(connectionString));
    services.AddHostedService<Worker>();
});

var host = builder.Build();

host.Run();