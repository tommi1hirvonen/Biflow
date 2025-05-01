using System.Diagnostics;
using System.Text;
using Biflow.Proxy.Core;

namespace Biflow.Proxy.WebApp.ProxyTasks;

public static class ProxyTask
{
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

            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, e) => outputBuilder.AppendLine(e.Data);
            process.ErrorDataReceived += (_, e) => errorBuilder.AppendLine(e.Data);
            
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            ExeProxyRunResult result;
            string? internalError = null;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    internalError = e.ToString();
                }
                catch (Exception ex)
                {
                    internalError = ex.ToString();
                }
            }
            catch (Exception e)
            {
                internalError = e.ToString();
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
                    ExitCode = process.ExitCode,
                    Output = output,
                    OutputIsTruncated = outputTruncated,
                    ErrorOutput = error,
                    ErrorOutputIsTruncated = errorTruncated,
                    InternalError = internalError
                };
            }
            
            return result;
        };
}