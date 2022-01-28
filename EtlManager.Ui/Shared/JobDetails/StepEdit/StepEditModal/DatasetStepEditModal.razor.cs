using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class DatasetStepEditModal : StepEditModalBase<DatasetStep>
{
    [Parameter] public IList<AppRegistration>? AppRegistrations { get; set; }

    private DatasetSelectOffcanvas DatasetSelectOffcanvas { get; set; } = null!;

    internal override string FormId => "dataset_step_edit_form";

    private async Task OpenDatasetSelectOffcanvas() => await DatasetSelectOffcanvas.ShowAsync();

    private void OnDatasetSelected((string GroupId, string DatasetId) dataset)
    {
        Step.DatasetGroupId = dataset.GroupId;
        Step.DatasetId = dataset.DatasetId;
    }

    protected override Task<DatasetStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId) =>
        context.DatasetSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .FirstAsync(step => step.StepId == stepId);

    protected override DatasetStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            AppRegistrationId = AppRegistrations?.FirstOrDefault()?.AppRegistrationId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>()
        };
}
