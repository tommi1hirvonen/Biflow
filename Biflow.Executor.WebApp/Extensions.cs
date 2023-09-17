using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.WebExtensions;

namespace Biflow.Executor.WebApp;

public static class Extensions
{
    public static WebApplication MapExecutorEndpoints(this WebApplication app)
    {
        app.MapGet("/execution/start/{executionId}", async (Guid executionId, IExecutionManager executionManager) =>
        {
            await executionManager.StartExecutionAsync(executionId);
        }).WithName("StartExecution");


        app.MapGet("/execution/stop/{executionId}", (Guid executionId, string username, IExecutionManager executionManager) =>
        {
            executionManager.CancelExecution(executionId, username);
        }).WithName("StopExecution");


        app.MapGet("/execution/stop/{executionId}/{stepId}", (Guid executionId, Guid stepId, string username, IExecutionManager executionManager) =>
        {
            executionManager.CancelExecution(executionId, username, stepId);
        }).WithName("StopExecutionStep");


        app.MapGet("/execution/status/{executionId}", (Guid executionId, IExecutionManager executionManager) =>
        {
            return executionManager.IsExecutionRunning(executionId)
                ? Results.Ok()
                : Results.NotFound();
        }).WithName("ExecutionStatus");


        app.MapGet("/connection/test", async (IConnectionTest connectionTest) =>
        {
            await connectionTest.RunAsync();
        }).WithName("TestConnection");


        app.MapPost("/email/test", async (string address, IEmailTest emailTest) =>
        {
            await emailTest.RunAsync(address);
            return Results.Ok("Email test succeeded. Check that the email was received successfully.");
        }).WithName("TestEmail");

        return app;
    }
}
