using JetBrains.Annotations;

namespace Biflow.ExecutorProxy.Core;

[PublicAPI]
public record ExeProxyRunRequest
{
    public required string ExePath { get; init; } = "";
    
    public string? Arguments { get; init; }
    
    public string? WorkingDirectory { get; init; }
}