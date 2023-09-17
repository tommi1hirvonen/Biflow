using Biflow.Scheduler.Core;
using Biflow.Scheduler.WebApp;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var useWindowsService = WindowsServiceHelpers.IsWindowsService();

WebApplicationBuilder builder;

// If hosted as a Windows service, configure specific logging and service lifetimes.
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
// Otherwise use default WebApplicationBuiderl.
else
{
    builder = WebApplication.CreateBuilder(args);
}

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();

var windowsAuth = builder.Configuration.GetSection("Authorization").GetSection("Windows");
var useWindowsAuth = windowsAuth.Exists();

if (useWindowsAuth)
{
    builder.Services.AddAuthorization(options =>
    {
        // If a list of Windows users were defined, require authentication for all endpoints.
        var allowedUsers = windowsAuth.GetSection("AllowedUsers").Get<string[]>();
        if (allowedUsers is null)
        {
            throw new ArgumentNullException(nameof(allowedUsers),
                "Property AllowedUsers must be defined if Windows Authorization is enabled");
        }
        options.FallbackPolicy = new AuthorizationPolicyBuilder().AddRequirements(new UserNamesRequirement(allowedUsers)).Build();
    });
    builder.Services.AddSingleton<IAuthorizationHandler, UserNamesHandler>();
}


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddSchedulerServices<WebAppExecutionJob>();
builder.Services.AddSingleton<StatusTracker>();

var app = builder.Build();

if (useWindowsAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

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
    catch
    {
        statusTracker.DatabaseReadError = true;
        throw;
    }
}).WithName("Synchronize");


app.MapGet("/status", (StatusTracker statusTracker) =>
{
    return statusTracker.DatabaseReadError
        ? throw new ApplicationException("Scheduler is running but has encountered a database read error.")
        : Results.Ok();
}).WithName("Status");


app.Run();


class UserNamesHandler : AuthorizationHandler<UserNamesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserNamesRequirement requirement)
    {
        var userName = context.User.Identity?.Name;

        if (userName is not null && requirement.UserNames.Contains(userName))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

class UserNamesRequirement : IAuthorizationRequirement
{
    public string[] UserNames { get; }

    public UserNamesRequirement(params string[] userNames)
    {
        UserNames = userNames;
    }
}