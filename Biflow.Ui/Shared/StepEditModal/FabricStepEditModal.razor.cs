using Biflow.Ui.Shared.StepEdit;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.DataPipeline.Models;
using Microsoft.Fabric.Api.Notebook.Models;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class FabricStepEditModal(ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<FabricStep>(toaster, dbContextFactory)
{
    internal override string FormId => "fabric_step_edit_form";

    private FabricItemSelectOffcanvas? _offcanvas;

    protected override FabricStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AzureCredentialId = AzureCredentials.First().AzureCredentialId
        };

    protected override Task<FabricStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.FabricSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
    
    private Task OpenItemSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _offcanvas.LetAsync(x => x.ShowAsync(Step.AzureCredentialId));   
    }

    private void OnItemSelected(FabricItemSelectedResult result)
    {
        ArgumentNullException.ThrowIfNull(Step);
        var (workspaceId, workspaceName, item) = result;
        if (!item.Id.HasValue)
        {
            throw new ArgumentNullException(nameof(item.Id));
        }
        Step.WorkspaceId = workspaceId;
        Step.WorkspaceName = workspaceName;
        switch (item)
        {
            case DataPipeline:
                Step.ItemId = (Guid)item.Id;
                Step.ItemName = item.DisplayName;
                Step.ItemType = FabricItemType.DataPipeline;
                break;
            case Notebook:
                Step.ItemId = (Guid)item.Id;
                Step.ItemName = item.DisplayName;
                Step.ItemType = FabricItemType.Notebook;
                break;
        }
    }
}