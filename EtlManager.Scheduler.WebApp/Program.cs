using EtlManager.Scheduler.Core;
using EtlManager.Scheduler.WebApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var useWindowsService = WindowsServiceHelpers.IsWindowsService();

WebApplicationBuilder builder;

if (useWindowsService)
{
    var options = new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    };
    builder = WebApplication.CreateBuilder(options);
    builder.Host.UseWindowsService();
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}
else
{
    builder = WebApplication.CreateBuilder(args);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

var connectionString = builder.Configuration.GetConnectionString("EtlManagerContext");
var executorType = builder.Configuration.GetSection("Executor").GetValue<string>("Type");
if (executorType == "ConsoleApp")
{
    builder.Services.AddSchedulerServices<ConsoleAppExecutionJob>(connectionString);
}
else if (executorType == "WebApp")
{
    builder.Services.AddSchedulerServices<WebAppExecutionJob>(connectionString);
}
else
{
    throw new ArgumentException($"Unrecognized executor type {executorType}");
}

builder.Services.AddSingleton<StatusTracker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Read all schedules into the schedules manager.
using (var scope = app.Services.CreateScope())
{
    var schedulesManager = scope.ServiceProvider.GetRequiredService<ISchedulesManager>();
    var statusTracker = scope.ServiceProvider.GetRequiredService<StatusTracker>();
    try
    {
        await schedulesManager.ReadAllSchedules(CancellationToken.None);
    }
    catch (Exception)
    {
        statusTracker.DatabaseReadError = true;
    }
}

app.MapPost("/schedules/add", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.AddScheduleAsync(schedule, CancellationToken.None);
}).WithName("Add schedule");


app.MapPost("/schedules/remove", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.RemoveScheduleAsync(schedule, CancellationToken.None);
}).WithName("Remove schedule");


app.MapPost("/jobs/remove", async (SchedulerJob job, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.RemoveJobAsync(job, CancellationToken.None);
}).WithName("Remove job");


app.MapPost("schedules/pause", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
}).WithName("Pause schedule");


app.MapPost("/schedules/resume", async (SchedulerSchedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.ResumeScheduleAsync(schedule, CancellationToken.None);
}).WithName("Resume schedule");


app.MapGet("/schedules/synchronize", async (ISchedulesManager schedulesManager, StatusTracker statusTracker) =>
{
    try
    {
        await schedulesManager.ReadAllSchedules(CancellationToken.None);
        statusTracker.DatabaseReadError = false;
    }
    catch (Exception)
    {
        statusTracker.DatabaseReadError = true;
        throw;
    }
}).WithName("Synchronize");


app.MapGet("/status", (StatusTracker StatusTracker) =>
{
    return StatusTracker.DatabaseReadError ? "FAILURE" : "SUCCESS";
}).WithName("Status");


app.Run();