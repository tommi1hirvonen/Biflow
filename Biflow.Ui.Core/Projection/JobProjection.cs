using Biflow.Core.Entities;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight Job class replacement which can be used to only load specific columns from the database
/// </summary>
public record JobProjection(Guid JobId, string JobName, bool UseDependencyMode, Guid? CategoryId, JobCategory? Category);
