using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DbNotebookStepParameter : StepParameterBase
{
    public DbNotebookStepParameter() : base(ParameterType.DatabricksNotebook)
    {
    }

    internal DbNotebookStepParameter(DbNotebookStepParameter other, DbNotebookStep step, Job? job) : base(other, step, job)
    {
        Step = step;
    }

    [JsonIgnore]
    public DbNotebookStep Step { get; set; } = null!;

    [JsonIgnore]
    public override Step BaseStep => Step;
}
