namespace Biflow.Ui.Core.Projection;

/// <summary>
/// Lightweight Job class replacement which can be used to only load specific columns from the database
/// </summary>
public record JobProjection(Guid JobId, string JobName, ExecutionMode ExecutionMode, Guid? CategoryId, JobCategory? Category);
