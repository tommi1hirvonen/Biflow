using EtlManager.DataAccess.Models;
using EtlManager.Scheduler.Core;
using EtlManager.Scheduler.WebApp;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
var connectionString = builder.Configuration.GetConnectionString("EtlManagerContext");
builder.Services.AddSchedulerServices<ExecutionJob>(connectionString);
builder.Services.AddSingleton<StatusTracker>();

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

app.MapPost("/schedules/add", async (Schedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.AddScheduleAsync(schedule, CancellationToken.None);
}).WithName("Add schedule");


app.MapPost("/schedules/remove", async (Schedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.RemoveScheduleAsync(schedule, CancellationToken.None);
}).WithName("Remove schedule");


app.MapPost("/jobs/remove", async (Job job, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.RemoveJobAsync(job, CancellationToken.None);
}).WithName("Remove job");


app.MapPost("schedules/pause", async (Schedule schedule, ISchedulesManager schedulesManager) =>
{
    await schedulesManager.PauseScheduleAsync(schedule, CancellationToken.None);
}).WithName("Pause schedule");


app.MapPost("/schedules/resume", async (Schedule schedule, ISchedulesManager schedulesManager) =>
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