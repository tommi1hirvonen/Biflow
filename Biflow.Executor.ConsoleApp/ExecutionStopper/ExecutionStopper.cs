using Biflow.Utilities;
using System.IO.Pipes;
using System.Text.Json;

namespace Biflow.Executor.ConsoleApp.ExecutionStopper;

internal class ExecutionStopper : IExecutionStopper
{
    public async Task<bool> RunAsync(string executionId, string? username, string? stepId)
    {
        try
        {
            // Connect to the pipe server set up by the executor process.
            using var pipeClient = new NamedPipeClientStream(".", executionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
            await pipeClient.ConnectAsync(10000); // wait for 10 seconds
            using var streamWriter = new StreamWriter(pipeClient);
            // Send cancel command.
            var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
            Guid? stepId_ = stepId is null ? null : Guid.Parse(stepId);
            var cancelCommand = new CancelCommand(stepId_, username_);
            var json = JsonSerializer.Serialize(cancelCommand);
            streamWriter.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping execution: {ex.Message}");
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
            return false;
        }

        Console.WriteLine("Command sent successfully. Execution cancellation started.");
        return true;
    }
}
