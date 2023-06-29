using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared.StepEditModal;

/// <summary>
/// Lightweight Job class replacement which can be used to only load specific columns from the database
/// </summary>
/// <param name="JobId"></param>
/// <param name="JobName"></param>
/// <param name="CategoryId"></param>
/// <param name="Category"></param>
public record JobSlim(Guid JobId, string JobName, Guid? CategoryId, JobCategory? Category);
