using System.Text.Json;
using Xunit;

namespace Biflow.Proxy.Core.Test;

public class ExeTaskStatusResponseTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    [Fact]
    public void TaskRunningStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "$type": "Running"
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskRunningStatusResponse();
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
    
    [Fact]
    public void TaskFailedStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "$type": "Failed",
                                  "ErrorMessage": "Test"
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskFailedStatusResponse { ErrorMessage = "Test" };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
    
    [Fact]
    public void TaskSucceededStatusJsonExpected()
    {
        const string expected = """
                                {
                                  "$type": "Succeeded",
                                  "Response": {
                                    "ExitCode": 0,
                                    "Output": "Test",
                                    "OutputIsTruncated": false,
                                    "ErrorOutput": "Test",
                                    "ErrorOutputIsTruncated": false,
                                    "InternalError": null
                                  }
                                }
                                """;
        ExeTaskStatusResponse status = new ExeTaskSucceededStatusResponse
        {
            Result = new ExeProxyRunResult
            {
                ExitCode = 0,
                Output = "Test",
                OutputIsTruncated = false,
                ErrorOutput = "Test",
                ErrorOutputIsTruncated = false,
                InternalError = null
            }
        };
        var json = JsonSerializer.Serialize(status, Options);
        Assert.Equal(expected, json);
    }
}