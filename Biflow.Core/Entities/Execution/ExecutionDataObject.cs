using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

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

    [JsonIgnore]
    public Execution Execution { get; private set; } = null!;

    public Guid ObjectId { get; private set; }

    [MaxLength(500)]
    public string ObjectUri { get; private set; } = string.Empty;

    public int MaxConcurrentWrites { get; private set; } = 1;

    [JsonIgnore]
    public IEnumerable<StepExecutionDataObject> StepExecutions { get; } = new List<StepExecutionDataObject>();
}