using Biflow.Executor.Core;
using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.WebExtensions;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExecutorServices<ExecutorLauncher>();
builder.Services.AddSingleton<ExecutionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/execution/start/{executionId}", async (Guid executionId, ExecutionManager executionManager) =>
{
    await executionManager.StartExecutionAsync(executionId);
    return "Execution started successfully";
}).WithName("StartExecution");


app.MapGet("/execution/stop/{executionId}", (Guid executionId, string username, ExecutionManager executionManager) =>
{
    executionManager.CancelExecution(executionId, username);
    return "Cancellation started successfully";
}).WithName("StopExecution");


app.MapGet("/execution/stop/{executionId}/{stepId}", (Guid executionId, Guid stepId, string username, ExecutionManager executionManager) =>
{
    executionManager.CancelExecution(executionId, username, stepId);
    return "Cancellation started successfully";
}).WithName("StopExecutionStep");


app.MapGet("/execution/status/{executionId}", (Guid executionId, ExecutionManager executionManager) =>
{
    if (executionManager.IsExecutionRunning(executionId))
    {
        return "RUNNING";
    }
    else
    {
        return "NOT RUNNING";
    }
}).WithName("ExecutionStatus");


app.MapGet("/connection/test", async (IConnectionTest connectionTest) =>
{
    try
    {
        await connectionTest.RunAsync();
        return "Connection test succeeded.";
    }
    catch (Exception ex)
    {
        return $"Connection test failed.\n{ex.Message}";
    }
}).WithName("TestConnection");


app.MapPost("/email/test", async (string address, IEmailTest emailTest) =>
{
    try
    {
        await emailTest.RunAsync(address);
        return "Email test succeeded. Check that the email was received successfully.";
    }
    catch (Exception ex)
    {
        return $"Sending test email failed.\n{ex.Message}";
    }
}).WithName("TestEmail");


app.Run();