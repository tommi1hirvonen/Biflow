using EtlManager.Executor.Core;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace EtlManager.Executor.ConsoleApp;

internal class ExecutorLauncher : IExecutorLauncher
{
    private static string ExecutorFilePath => Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new ArgumentNullException("FileName", "Executor file path cannot be null");

    private Process? ExecutorProcess { get; set; }

    public Task StartExecutorAsync(Guid executionId, bool notify)
    {
        var executionInfo = new ProcessStartInfo()
        {
            FileName = ExecutorFilePath,
            ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString(),
                        notify ? "--notify" : ""
                    },
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        ExecutorProcess = new Process() { StartInfo = executionInfo };
        ExecutorProcess.Start();
        return Task.CompletedTask;
    }

    public async Task WaitForExitAsync(Guid _, CancellationToken cancellationToken) =>
        await (ExecutorProcess?.WaitForExitAsync(cancellationToken) ?? Task.CompletedTask);

    public async Task CancelAsync(Guid executionId, string username)
    {
        // Connect to the pipe server set up by the executor process.
        using var pipeClient = new NamedPipeClientStream(".", executionId.ToString().ToLower(), PipeDirection.Out); // "." => the pipe server is on the same computer
        await pipeClient.ConnectAsync(10000); // wait for 10 seconds
        using var streamWriter = new StreamWriter(pipeClient);
        // Send cancel command.
        var username_ = string.IsNullOrWhiteSpace(username) ? "unknown" : username;
        var cancelCommand = new { StepId = (string?)null, Username = username_ };
        var json = JsonSerializer.Serialize(cancelCommand);
        streamWriter.WriteLine(json);
    }
}
