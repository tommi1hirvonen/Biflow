using System.ComponentModel.DataAnnotations;

namespace Biflow.Executor.Core;

internal class ExecutionOptions
{
    [Range(1, int.MaxValue)]
    public int MaximumParallelSteps { get; set; }

    [Range(500, 30000)]
    public int PollingIntervalMs { get; set; }
}
