namespace Biflow.Ui.Api.Models;

/// <summary>
/// Request body for searching executions with filters and pagination.
/// </summary>
public class SearchExecutionsRequest
{
    /// <summary>
    /// Filter executions by job IDs.
    /// </summary>
    public Guid[]? JobIds { get; set; }

    /// <summary>
    /// Filter executions by schedule IDs.
    /// </summary>
    public Guid[]? ScheduleIds { get; set; }

    /// <summary>
    /// Filter executions by execution status.
    /// </summary>
    public ExecutionStatus[]? ExecutionStatuses { get; set; }

    /// <summary>
    /// Filter executions by start date (inclusive).
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter executions by end date (inclusive).
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Maximum number of results to return. Must be between 10 and 100.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// For pagination: the CreatedOn timestamp of the last item from the previous request.
    /// Must be provided together with LastExecutionId.
    /// </summary>
    public DateTimeOffset? LastCreatedOn { get; set; }

    /// <summary>
    /// For pagination: the ID of the last execution from the previous request.
    /// Must be provided together with LastCreatedOn.
    /// </summary>
    public Guid? LastExecutionId { get; set; }

    /// <summary>
    /// Whether to include the execution parameters in the response.
    /// </summary>
    public bool IncludeParameters { get; set; } = false;
}
