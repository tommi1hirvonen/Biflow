using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionDataObject")]
[PrimaryKey("ExecutionId", "ObjectId")]
public class ExecutionDataObject
{
    public ExecutionDataObject() { }

    public ExecutionDataObject(DataObject dataObject, Execution execution)
    {
        ExecutionId = execution.ExecutionId;
        ObjectId = dataObject.ObjectId;
        ObjectUri = dataObject.ObjectUri;
        MaxConcurrentWrites = dataObject.MaxConcurrentWrites;
    }

    public Guid ExecutionId { get; private set; }

    public Guid ObjectId { get; private set; }

    public string ObjectUri { get; private set; } = string.Empty;

    public int MaxConcurrentWrites { get; private set; } = 1;

    public IList<StepExecutionDataObject> StepExecutions { get; set; } = null!;
}