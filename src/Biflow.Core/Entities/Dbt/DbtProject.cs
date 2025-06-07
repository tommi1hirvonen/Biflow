using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DbtProject(long Id, string Name);