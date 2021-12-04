using EtlManager.Executor.Core;
using EtlManager.Executor.Core.ConnectionTest;
using EtlManager.Executor.Core.JobExecutor;
using EtlManager.Executor.Core.Notification;
using EtlManager.Executor.WebApp;
using EtlManager.Utilities;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var connectionString = builder.Configuration.GetConnectionString("EtlManagerContext");
builder.Services.AddExecutorServices<ExecutorLauncher>(connectionString);
builder.Services.AddSingleton<ExecutionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/execution/start", (StartCommand command, ExecutionManager executionManager) =>
{
    executionManager.StartExecution(command.ExecutionId, command.Notify, command.NotifyMe, command.NotifyMeOvertime);
    return "Execution started successfully";
}).WithName("StartExecution");


app.MapPost("/execution/stop", (StopCommand command, ExecutionManager executionManager) =>
{
    if (command.StepId is not null)
    {
        executionManager.CancelExecution(command.ExecutionId, command.Username, (Guid)command.StepId);
    }
    else
    {
        executionManager.CancelExecution(command.ExecutionId, command.Username);
    }
    return "Cancellation started successfully";
}).WithName("StopExecution");


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