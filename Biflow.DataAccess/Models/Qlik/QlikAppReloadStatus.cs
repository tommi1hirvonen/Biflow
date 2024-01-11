namespace Biflow.DataAccess.Models;

public enum QlikAppReloadStatus
{
    Queued,
    Reloading,
    Canceling,
    Succeeded,
    Failed,
    Canceled,
    ExceededLimit
}