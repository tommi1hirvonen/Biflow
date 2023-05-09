namespace Biflow.DataAccess.Models;

/// <summary>
/// Static class to contain constant strings used as names for defining job and step expression parameters
/// </summary>
public static class ExpressionParameterNames
{
    public const string ExecutionId = "_execution_id_";

    public const string JobId = "_job_id_";

    public const string StepId = "_step_id_";

    public const string RetryAttemptIndex = "_retry_attempt_index_";
}
