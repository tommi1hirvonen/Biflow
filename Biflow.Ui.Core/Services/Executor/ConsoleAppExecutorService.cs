using Biflow.DataAccess.Models;
using Biflow.Utilities;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace Biflow.Ui.Core;

public class ConsoleAppExecutorService : IExecutorService
{
    private readonly IConfiguration _configuration;

    private string ExecutorPath => _configuration
        .GetSection("Executor")
        .GetSection("ConsoleApp")
        .GetValue<string>("BiflowExecutorPath");

    public ConsoleAppExecutorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task StartExecutionAsync(Guid executionId)
    {
        var executionInfo = new ProcessStartInfo()
        {
            // The installation folder should be included in the Path variable, so no path required here.
            FileName = ExecutorPath,
            ArgumentList = {
                    "execute",
                    "--id",
                    executionId.ToString()
                },
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var executorProcess = new Process() { StartInfo = executionInfo };
        executorProcess.Start();

        return Task.CompletedTask;
    }

    public async Task StopExecutionAsync(StepExecutionAttempt attempt, string username)
    {
        // Connect to the pipe server set up by the executor process.
        using var pipeClient = new NamedPipeClientStream(".", attempt.ExecutionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
        using var streamWriter = new StreamWriter(pipeClient);
        // Send cancel command.
        var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
        var cancelCommand = new CancelCommand(attempt.StepId, username);
        var json = JsonSerializer.Serialize(cancelCommand);
        streamWriter.WriteLine(json);
    }

    public async Task StopExecutionAsync(Execution execution, string username)
    {
        // Connect to the pipe server set up by the executor process.
        using var pipeClient = new NamedPipeClientStream(".", execution.ExecutionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
        using var streamWriter = new StreamWriter(pipeClient);
        // Send cancel command.
        var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
        var cancelCommand = new CancelCommand(null, username);
        var json = JsonSerializer.Serialize(cancelCommand);
        streamWriter.WriteLine(json);
    }
}
