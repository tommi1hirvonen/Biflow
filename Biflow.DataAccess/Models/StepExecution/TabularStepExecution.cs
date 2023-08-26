using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class TabularStepExecution : StepExecution, IHasTimeout
{
    public TabularStepExecution(string stepName, string tabularModelName)
        : base(stepName, StepType.Tabular)
    {
        TabularModelName = tabularModelName;
    }

    public TabularStepExecution(TabularStep step, Execution execution) : base(step, execution)
    {
        TabularModelName = step.TabularModelName;
        TabularTableName = step.TabularTableName;
        TabularPartitionName = step.TabularPartitionName;
        TimeoutMinutes = step.TimeoutMinutes;
        ConnectionId = step.ConnectionId;
        Connection = step.Connection;

        StepExecutionAttempts = new[] { new TabularStepExecutionAttempt(this) };
    }

    [Display(Name = "Model name")]
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string TabularModelName { get; private set; }

    [Display(Name = "Table name")]
    [MinLength(1)]
    [MaxLength(128)]
    public string? TabularTableName { get; private set; }

    [Display(Name = "Partition name")]
    [MinLength(1)]
    [MaxLength(128)]
    public string? TabularPartitionName { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; private set; }

    public AnalysisServicesConnectionInfo Connection { get; set; } = null!;

}
