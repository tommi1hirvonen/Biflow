using System.Text.Json;
using Xunit;

namespace Biflow.ExecutorProxy.Core.Test;

public class ExeTaskStatusResponseTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    [Fact]
    public void TaskRunningStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "status": "running",
                                  "processId": 0,
                                  "output": "Test",
                                  "outputIsTruncated": false,
                                  "errorOutput": "Test",
                                  "errorOutputIsTruncated": false
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskRunningResponse
        {
            ProcessId = 0,
            Output = "Test",
            OutputIsTruncated = false,
            ErrorOutput = "Test",
            ErrorOutputIsTruncated = false
        };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
    
    [Fact]
    public void TaskFailedStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "status": "failed",
                                  "errorMessage": "Test"
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskFailedResponse { ErrorMessage = "Test" };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
    
    [Fact]
    public void TaskSucceededStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "status": "completed",
                                  "processId": 0,
                                  "exitCode": 0,
                                  "output": "Test",
                                  "outputIsTruncated": false,
                                  "errorOutput": "Test",
                                  "errorOutputIsTruncated": false,
                                  "internalError": null
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskCompletedResponse
        {
            ProcessId = 0,
            ExitCode = 0,
            Output = "Test",
            OutputIsTruncated = false,
            ErrorOutput = "Test",
            ErrorOutputIsTruncated = false,
            InternalError = null
        };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
}