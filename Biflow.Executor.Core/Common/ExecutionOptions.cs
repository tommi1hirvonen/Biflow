using System.ComponentModel.DataAnnotations;

namespace Biflow.Executor.Core;

internal class ExecutionOptions
{
    [Range(500, 30000)]
    public int PollingIntervalMs { get; set; }
}
