using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class SqlStepEditModal : ParameterizedStepEditModal<SqlStep>
{

    internal override string FormId => "sql_step_edit_form";

    private StoredProcedureSelectOffcanvas StoredProcedureSelectModal { get; set; } = null!;
    
    protected override Task<SqlStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.SqlSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override SqlStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<StepParameterBase>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    private void SetCaptureResultJobParameter(bool enabled)
    {
        if (enabled)
        {
            Step.ResultCaptureJobParameterId = Job?.JobParameters.FirstOrDefault()?.ParameterId;
        }
        else
        {
            Step.ResultCaptureJobParameterId = null;
        }
    }

    private Task OpenStoredProcedureSelectModal() => StoredProcedureSelectModal.ShowAsync();

    private void OnStoredProcedureSelected(string procedure) => Step.SqlStatement = $"EXEC {procedure}";
    
}
