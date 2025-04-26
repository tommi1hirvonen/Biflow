using JetBrains.Annotations;

namespace Biflow.Proxy.Core;

[PublicAPI]
public record RunProxyExeRequest
{
    public required string ExePath { get; init; } = "";
    
    public string? Arguments { get; init; }
    
    public string? WorkingDirectory { get; init; }
}