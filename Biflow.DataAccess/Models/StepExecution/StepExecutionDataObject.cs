using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepDataObject")]
[PrimaryKey("ExecutionId", "StepId", "ObjectId")]
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

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public Guid ObjectId { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public ExecutionDataObject DataObject { get; set; } = null!;

    [MaxLength(20)]
    [Unicode(false)]
    public DataObjectReferenceType ReferenceType { get; set; }

    public List<string> DataAttributes { get; set; } = [];
}
