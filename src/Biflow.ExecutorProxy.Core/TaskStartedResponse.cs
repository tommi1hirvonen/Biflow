using JetBrains.Annotations;

namespace Biflow.ExecutorProxy.Core;

[PublicAPI]
public record TaskStartedResponse(Guid TaskId);