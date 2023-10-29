using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal : StepEditModal<PackageStep>
{
    internal override string FormId => "package_step_edit_form";

    private PackageSelectOffcanvas? packageSelectOffcanvas;

    protected override PackageStep CreateNewStep(Job job) =>
        new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            StepParameters = new List<PackageStepParameter>(),
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<PackageStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.PackageSteps
        .Include(step => step.Job)
        .ThenInclude(job => job.JobParameters)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.ExpressionParameters)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenPackageSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step?.ConnectionId);
        return packageSelectOffcanvas.LetAsync(x => x.ShowAsync((Guid)Step.ConnectionId));
    }

    private void OnPackageSelected(CatalogPackage package)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PackageFolderName = package.Project.Folder.FolderName;
        Step.PackageProjectName = package.Project.ProjectName;
        Step.PackageName = package.PackageName;
    }
}
