using System.Diagnostics;
using System.Text;
using Biflow.Proxy.Core;

namespace Biflow.Proxy.WebApp.ProxyTasks;

internal class ExeProxyTask(ExeProxyRunRequest request) : ProxyTask<ExeTaskRunningStatusResponse, ExeProxyRunResult>
{
    private int _processId;
    
    public override ExeTaskRunningStatusResponse Status => new()
    {
        ProcessId = _processId
    };

    public override async Task<ExeProxyRunResult> RunAsync(CancellationToken cancellationToken)
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
        
        try
        {
            _processId = process.Id;
        }
        catch (Exception e)
        {
            _processId = -1;
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
                ProcessId = _processId,
                ExitCode = process.ExitCode,
                Output = output,
                OutputIsTruncated = outputTruncated,
                ErrorOutput = error,
                ErrorOutputIsTruncated = errorTruncated,
                InternalError = internalErrorBuilder.ToString(),
            };
        }
        
        return result;
    }
}