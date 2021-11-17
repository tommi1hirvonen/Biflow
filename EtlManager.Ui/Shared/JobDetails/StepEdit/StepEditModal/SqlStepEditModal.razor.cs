using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class SqlStepEditModal : ParameterizedStepEditModal<SqlStep>
{
    [Parameter] public IList<SqlConnectionInfo>? Connections { get; set; }

    internal override string FormId => "sql_step_edit_form";

    private StoredProcedureSelectOffcanvas StoredProcedureSelectModal { get; set; } = null!;
    
    private SqlReferenceExplorerOffcanvas SqlReferenceOffcanvas { get; set; } = null!;
    
    private SqlDefinitionExplorerOffcanvas SqlDefinitionOffcanvas { get; set; } = null!;

    protected override async Task<SqlStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId)
    {
        return await context.SqlSteps
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.JobParameter)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .FirstAsync(step => step.StepId == stepId);
    }

    protected override SqlStep CreateNewStep(Job job)
    {
        return new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<StepParameterBase>()
        };
    }

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

    private async Task OpenStoredProcedureSelectModal() => await StoredProcedureSelectModal.ShowAsync();

    private void OnStoredProcedureSelected(string procedure) => Step.SqlStatement = $"EXEC {procedure}";
    
}
