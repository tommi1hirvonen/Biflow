using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepSource")]
[PrimaryKey("ExecutionId", "StepId", "ObjectId")]
public class StepExecutionSource
{
    public StepExecutionSource() { }

    public StepExecutionSource(StepExecution step, ExecutionDataObject dataObject)
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
