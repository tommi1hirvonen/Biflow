using System.Diagnostics;
using System.Text;
using Biflow.Proxy.Core;

namespace Biflow.Proxy.WebApp.ProxyTasks;

public static class ProxyTask
{
    /// <summary>
    /// Creates a delegate function that executes a proxy-based executable task.
    /// </summary>
    /// <param name="request">
    /// The request specifying the executable path and optional arguments and working directory for the task.
    /// </param>
    /// <returns>
    /// A delegate function that accepts a <see cref="CancellationToken"/> and returns a <see cref="Task"/>
    /// with the result of type <see cref="ExeProxyRunResult"/>.
    /// </returns>
    public static Func<CancellationToken, Task<ExeProxyRunResult>> Create(ExeProxyRunRequest request) =>
        async cancellationToken =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = request.ExePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = string.IsNullOrWhiteSpace(request.Arguments) ? "" : request.Arguments,
                WorkingDirectory = string.IsNullOrEmpty(request.WorkingDirectory) ? "" : request.WorkingDirectory
            };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var internalErrorBuilder = new StringBuilder();

            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, e) => outputBuilder.AppendLine(e.Data);
            process.ErrorDataReceived += (_, e) => errorBuilder.AppendLine(e.Data);
            
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            int processId;
            try
            {
                processId = process.Id;
            }
            catch (Exception e)
            {
                processId = -1;
                internalErrorBuilder.AppendLine($"Failed to get process ID\n{e}");
            }

            ExeProxyRunResult result;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    internalErrorBuilder.AppendLine(e.ToString());
                }
                catch (Exception ex)
                {
                    internalErrorBuilder.AppendLine(ex.ToString());
                }
            }
            catch (Exception e)
            {
                internalErrorBuilder.AppendLine(e.ToString());
            }
            finally
            {
                var (output, outputTruncated) = outputBuilder.ToString() switch
                {
                    { Length: > 500_000 } s1 => (s1[..Math.Min(500_000, s1.Length)], true),
                    { Length: > 0 } s2 => (s2, false),
                    _ => (null, false)
                };
                var (error, errorTruncated) = errorBuilder.ToString() switch
                {
                    { Length: > 500_000 } s1 => (s1[..Math.Min(500_000, s1.Length)], true),
                    { Length: > 0 } s2 => (s2, false),
                    _ => (null, false)
                };
                result = new ExeProxyRunResult
                {
                    ProcessId = processId,
                    ExitCode = process.ExitCode,
                    Output = output,
                    OutputIsTruncated = outputTruncated,
                    ErrorOutput = error,
                    ErrorOutputIsTruncated = errorTruncated,
                    InternalError = internalErrorBuilder.ToString(),
                };
            }
            
            return result;
        };
}