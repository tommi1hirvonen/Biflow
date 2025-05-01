using JetBrains.Annotations;

namespace Biflow.Proxy.Core;

[PublicAPI]
public record TaskStartedResponse(Guid TaskId);