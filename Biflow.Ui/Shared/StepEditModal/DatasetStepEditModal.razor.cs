using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal : StepEditModal<DatasetStep>
{
    [Parameter] public IList<AppRegistration>? AppRegistrations { get; set; }

    private DatasetSelectOffcanvas? DatasetSelectOffcanvas { get; set; }

    internal override string FormId => "dataset_step_edit_form";

    private Task OpenDatasetSelectOffcanvas() => DatasetSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private void OnDatasetSelected((string GroupId, string DatasetId) dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.DatasetGroupId = dataset.GroupId;
        Step.DatasetId = dataset.DatasetId;
    }

    protected override Task<DatasetStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.DatasetSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
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
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
}
