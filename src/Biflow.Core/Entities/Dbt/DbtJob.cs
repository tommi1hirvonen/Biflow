using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DbtJob(long Id, string Name);