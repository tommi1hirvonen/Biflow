using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepExecutionDataObject
{
    public StepExecutionDataObject() { }

    public StepExecutionDataObject(
        StepExecution step,
        ExecutionDataObject dataObject,
        DataObjectReferenceType referenceType,
        IEnumerable<string> dataAttributes)
    {
        StepExecution = step;
        DataObject = dataObject;
        ExecutionId = step.ExecutionId;
        StepId = step.StepId;
        ObjectId = dataObject.ObjectId;
        ReferenceType = referenceType;
        DataAttributes = dataAttributes.ToList();
    }

    public Guid ExecutionId { get; init; }

    public Guid StepId { get; init; }

    public Guid ObjectId { get; init; }

    [JsonIgnore]
    public StepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public ExecutionDataObject DataObject { get; init; } = null!;

    public DataObjectReferenceType ReferenceType { get; init; }

    public List<string> DataAttributes { get; init; } = [];
}
