namespace Biflow.Core.Entities;

public enum DatasetRefreshStatus
{
    /// <summary>
    /// Completion state is unknown or a refresh is in progress
    /// </summary>
    Unknown,
    /// <summary>
    /// Refresh completed successfully
    /// </summary>
    Completed,
    /// <summary>
    /// Refresh was unsuccessful
    /// </summary>
    Failed,
    /// <summary>
    /// Refresh is disabled by a selective refresh
    /// </summary>
    Disabled
}