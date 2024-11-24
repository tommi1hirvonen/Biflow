using Biflow.Ui.Shared.StepEdit;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal(
    ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<PackageStep>(toaster, dbContextFactory)
{
    internal override string FormId => "package_step_edit_form";

    private PackageSelectOffcanvas? _packageSelectOffcanvas;
    
    private MsSqlConnection? Connection
    {
        get
        {
            if (field is null || field.ConnectionId != Step?.ConnectionId)
            {
                field = MsSqlConnections
                            .FirstOrDefault(c => c.ConnectionId == Step?.ConnectionId)
                        ?? MsSqlConnections.First();
            }
            return field;
        }
    }

    protected override PackageStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = MsSqlConnections.First().ConnectionId
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
        .Include(step => step.DataObjects)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenPackageSelectOffcanvas()
    {
        var connection = Connection;
        ArgumentNullException.ThrowIfNull(connection);
        return _packageSelectOffcanvas.LetAsync(x => x.ShowAsync(connection));
    }

    private void OnPackageSelected(CatalogPackage package)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PackageFolderName = package.Project.Folder.FolderName;
        Step.PackageProjectName = package.Project.ProjectName;
        Step.PackageName = package.PackageName;
    }
}
