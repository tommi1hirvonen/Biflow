using System.Diagnostics;
using System.Text;
using Biflow.ExecutorProxy.Core;

namespace Biflow.ExecutorProxy.WebApp.ProxyTasks;

internal class ExeProxyTask(ExeProxyRunRequest request) : IProxyTask<ExeTaskRunningResponse, ExeTaskCompletedResponse>
{
    private int _processId;
    private readonly StringBuilder _outputBuilder = new();
    private readonly StringBuilder _errorBuilder = new();
    
    // Use locks to ensure thread-safety when accessing the output and error string builders.
    // The string builders are not thread-safe by default, and status may be accessed from other threads.
    private readonly ReaderWriterLockSlim _outputLock = new();
    private readonly ReaderWriterLockSlim _errorLock = new();
    
    private const int MaxOutputLength = 500_000;

    public ExeTaskRunningResponse Status
    {
        get
        {
            var (output, outputTruncated) = CalculateOutput();
            var (error, errorTruncated) = CalculateErrorOutput();
            return new ExeTaskRunningResponse
            {
                ProcessId = _processId,
                Output = output,
                OutputIsTruncated = outputTruncated,
                ErrorOutput = error,
                ErrorOutputIsTruncated = errorTruncated,
            };
        }
    }

    public async Task<ExeTaskCompletedResponse> RunAsync(CancellationToken cancellationToken)
    {
        _outputBuilder.Clear();
        _errorBuilder.Clear();
        
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

        var internalErrorBuilder = new StringBuilder();

        using var process = new Process();
        process.StartInfo = startInfo;
        
        // Register thread-safe event handlers for output and error messages.
        process.OutputDataReceived += OutputDataReceived;
        process.ErrorDataReceived += ErrorDataReceived;
        
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

        ExeTaskCompletedResponse result;
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
            var (output, outputTruncated) = CalculateOutput();
            var (error, errorTruncated) = CalculateErrorOutput();
            result = new ExeTaskCompletedResponse
            {
                ProcessId = _processId,
                ExitCode = process.ExitCode,
                Output = output,
                OutputIsTruncated = outputTruncated,
                ErrorOutput = error,
                ErrorOutputIsTruncated = errorTruncated,
                InternalError = internalErrorBuilder.ToString() is { Length: > 0 } s ? s : null
            };
        }
        
        return result;
    }

    private void OutputDataReceived(object _, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;
        
        try
        {
            _outputLock.EnterWriteLock();
            _outputBuilder.AppendLine(e.Data);
        }
        finally
        {
            _outputLock.ExitWriteLock();
        }
    }

    private void ErrorDataReceived(object _, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;
        
        try
        {
            _errorLock.EnterWriteLock();
            _errorBuilder.AppendLine(e.Data);
        }
        finally
        {
            _errorLock.ExitWriteLock();
        }
    }

    private (string? Output, bool IsTruncated) CalculateOutput()
    {
        string output;
        try
        {
            _outputLock.EnterReadLock();
            output = _outputBuilder.ToString();
        }
        finally
        {
            _outputLock.ExitReadLock();   
        }
        return output switch
        {
            { Length: > MaxOutputLength } => (output[..Math.Min(MaxOutputLength, output.Length)], true),
            { Length: > 0 } => (output, false),
            _ => (null, false)
        };
    }

    private (string? ErrorOutput, bool IsTruncated) CalculateErrorOutput()
    {
        string error;
        try
        {
            _errorLock.EnterReadLock();
            error = _errorBuilder.ToString();
        }
        finally
        {
            _errorLock.ExitReadLock();
        }
        return error switch
        {
            { Length: > MaxOutputLength } => (error[..Math.Min(MaxOutputLength, error.Length)], true),
            { Length: > 0 } => (error, false),
            _ => (null, false)
        };
    }
}