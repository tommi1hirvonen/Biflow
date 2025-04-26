using JetBrains.Annotations;

namespace Biflow.Proxy.Core;

[PublicAPI]
public record RunProxyExeResponse
{
    public required int ExitCode { get; init; }
    
    public required string? Output { get; init; }
    
    public required bool OutputIsTruncated { get; init; }
    
    public required string? ErrorOutput { get; init; }
    
    public required bool ErrorOutputIsTruncated { get; init; }
    
    public required string? InternalError { get; init; }
}