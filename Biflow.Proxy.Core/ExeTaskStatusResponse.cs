using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Proxy.Core;

[JsonDerivedType(typeof(ExeTaskRunningStatusResponse), "Running")]
[JsonDerivedType(typeof(ExeTaskSucceededStatusResponse), "Succeeded")]
[JsonDerivedType(typeof(ExeTaskFailedStatusResponse), "Failed")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "status")]
public abstract class ExeTaskStatusResponse;

[PublicAPI]
public class ExeTaskRunningStatusResponse : ExeTaskStatusResponse
{
    public required int ProcessId { get; init; }
}

[PublicAPI]
public class ExeTaskSucceededStatusResponse : ExeTaskStatusResponse
{
    public required ExeProxyRunResult Result { get; init; }
}

[PublicAPI]
public class ExeTaskFailedStatusResponse : ExeTaskStatusResponse
{
    public required string ErrorMessage { get; init; }
}