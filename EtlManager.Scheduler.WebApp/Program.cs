using EtlManager.DataAccess;
using EtlManager.Scheduler.Core;
using EtlManager.Scheduler.WebApp;
using EtlManager.Utilities;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
var connectionString = builder.Configuration.GetConnectionString("EtlManagerContext");
builder.Services.AddDbContextFactory<EtlManagerContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<SchedulesManager<WebAppExecutionJob>>();
builder.Services.AddSingleton<StatusTracker>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// Read all schedules into the schedules manager.
using (var scope = app.Services.CreateScope())
{
    var schedulesManager = scope.ServiceProvider.GetRequiredService<SchedulesManager<WebAppExecutionJob>>();
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

app.MapPost("/scheduler", async (SchedulerCommand command, SchedulesManager<WebAppExecutionJob> schedulesManager, StatusTracker statusTracker) =>
{
    switch (command.Type)
    {
        case SchedulerCommand.CommandType.Add:
            await schedulesManager.AddScheduleAsync(command, CancellationToken.None);
            break;
        case SchedulerCommand.CommandType.Delete:
            await schedulesManager.RemoveScheduleAsync(command, CancellationToken.None);
            break;
        case SchedulerCommand.CommandType.Pause:
            await schedulesManager.PauseScheduleAsync(command, CancellationToken.None);
            break;
        case SchedulerCommand.CommandType.Resume:
            await schedulesManager.ResumeScheduleAsync(command, CancellationToken.None);
            break;
        case SchedulerCommand.CommandType.Synchronize:
            try
            {
                await schedulesManager.ReadAllSchedules(CancellationToken.None);
                statusTracker.DatabaseReadError = false;
            }
            catch (Exception)
            {
                statusTracker.DatabaseReadError = true;
                return "FAILURE";
            }
            break;
        case SchedulerCommand.CommandType.Status:
            if (statusTracker.DatabaseReadError)
            {
                return "FAILURE";
            }
            else
            {
                return "SUCCESS";
            }
        default:
            throw new ArgumentException($"Invalid command type {command.Type}");
    }
    return "SUCCESS";
})
.WithName("Scheduler");

app.Run();