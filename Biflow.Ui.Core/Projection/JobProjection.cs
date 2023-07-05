using Biflow.DataAccess.Models;

namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight Job class replacement which can be used to only load specific columns from the database
/// </summary>
public record JobProjection(Guid JobId, string JobName, Guid? CategoryId, JobCategory? Category);
