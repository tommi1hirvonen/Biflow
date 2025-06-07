using System.Text.Json;
using Xunit;

namespace Biflow.ExecutorProxy.Core.Test;

public class ExeTaskStatusResponseTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    [Fact]
    public void TaskRunningStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "status": "Running",
                                  "ProcessId": 0
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskRunningResponse { ProcessId = 0 };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
    
    [Fact]
    public void TaskFailedStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "status": "Failed",
                                  "ErrorMessage": "Test"
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
                                  "status": "Completed",
                                  "ProcessId": 0,
                                  "ExitCode": 0,
                                  "Output": "Test",
                                  "OutputIsTruncated": false,
                                  "ErrorOutput": "Test",
                                  "ErrorOutputIsTruncated": false,
                                  "InternalError": null
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