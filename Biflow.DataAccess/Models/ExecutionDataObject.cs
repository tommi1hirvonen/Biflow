using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionDataObject")]
[PrimaryKey("ExecutionId", "ObjectId")]
public class ExecutionDataObject
{
    public Guid ExecutionId { get; private set; }

    public Guid ObjectId { get; private set; }

    public string ServerName { get; private set; } = string.Empty;

    public string DatabaseName { get; private set; } = string.Empty;

    public string SchemaName { get; private set; } = string.Empty;

    public string ObjectName { get; private set; } = string.Empty;

    public int MaxConcurrentWrites { get; private set; } = 1;

    public IList<StepExecution> Targets { get; set; } = null!;

    public IList<StepExecution> Sources { get; set; } = null!;
}