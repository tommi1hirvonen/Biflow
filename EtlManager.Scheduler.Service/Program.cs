using EtlManager.DataAccess;
using EtlManager.Scheduler;
using EtlManager.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

var builder = Host.CreateDefaultBuilder(args);

builder.UseWindowsService();

builder.ConfigureLogging((hostContext, loggingBuilder) =>
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger();
    loggingBuilder.AddSerilog(logger, dispose: true);
});

builder.ConfigureServices((hostContext, services) =>
{
    var connectionString = hostContext.Configuration.GetConnectionString("EtlManagerContext");
    services.AddSchedulerServices<ExecutionJob>(connectionString);
    services.AddHostedService<Worker>();
});

var host = builder.Build();

host.Run();