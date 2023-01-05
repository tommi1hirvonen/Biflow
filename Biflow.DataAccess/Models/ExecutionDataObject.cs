using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionDataObject")]
[PrimaryKey("ExecutionId", "ObjectId")]
public class ExecutionDataObject
{
    public Guid ExecutionId { get; set; }

    public Guid ObjectId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string SchemaName { get; set; } = string.Empty;

    public string ObjectName { get; set; } = string.Empty;

    public int MaxConcurrentWrites { get; set; } = 1;

    public IList<StepExecution> Targets { get; set; } = null!;

    public IList<StepExecution> Sources { get; set; } = null!;
}