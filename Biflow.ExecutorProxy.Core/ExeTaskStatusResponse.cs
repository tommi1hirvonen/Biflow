using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.ExecutorProxy.Core;

[JsonDerivedType(typeof(ExeTaskRunningResponse), "Running")]
[JsonDerivedType(typeof(ExeTaskCompletedResponse), "Completed")]
[JsonDerivedType(typeof(ExeTaskFailedResponse), "Failed")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "status")]
public abstract class ExeTaskStatusResponse;

[PublicAPI]
public class ExeTaskRunningResponse : ExeTaskStatusResponse
{
    public required int ProcessId { get; init; }
}

[PublicAPI]
public class ExeTaskCompletedResponse : ExeTaskStatusResponse
{
    public required int ProcessId { get; init; }
    
    public required int ExitCode { get; init; }
    
    public required string? Output { get; init; }
    
    public required bool OutputIsTruncated { get; init; }
    
    public required string? ErrorOutput { get; init; }
    
    public required bool ErrorOutputIsTruncated { get; init; }
    
    public required string? InternalError { get; init; }
}

[PublicAPI]
public class ExeTaskFailedResponse : ExeTaskStatusResponse
{
    public required string ErrorMessage { get; init; }
}