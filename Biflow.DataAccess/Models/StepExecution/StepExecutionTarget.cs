using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepTarget")]
[PrimaryKey("ExecutionId", "StepId", "ObjectId")]
public class StepExecutionTarget
{
    public StepExecutionTarget() { }

    public StepExecutionTarget(StepExecution step, ExecutionDataObject dataObject)
    {
        StepExecution = step;
        DataObject = dataObject;
        ExecutionId = step.ExecutionId;
        StepId = step.StepId;
        ObjectId = dataObject.ObjectId;
    }

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public Guid ObjectId { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public ExecutionDataObject DataObject { get; set; } = null!;
}
