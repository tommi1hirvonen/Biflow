using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DbtEnvironment(long Id, string Name);