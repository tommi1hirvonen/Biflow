using Biflow.Ui.Shared.StepEdit;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<PackageStep>(mediator, toaster, dbContextFactory)
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
    
    protected override async Task<PackageStep> OnSubmitCreateAsync(PackageStep step)
    {
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new CreateExecutionConditionParameter(
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var parameters = step.StepParameters
            .Select(p => new CreatePackageStepParameter(
                p.ParameterName,
                p.ParameterLevel,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new CreatePackageStepCommand
        {
            JobId = step.JobId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            ConnectionId = step.ConnectionId,
            PackageFolderName = step.PackageFolderName,
            PackageProjectName = step.PackageProjectName,
            PackageName = step.PackageName,
            ExecuteIn32BitMode = step.ExecuteIn32BitMode,
            ExecuteAsLogin = step.ExecuteAsLogin,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<PackageStep> OnSubmitUpdateAsync(PackageStep step)
    {
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new UpdateExecutionConditionParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var parameters = step.StepParameters
            .Select(p => new UpdatePackageStepParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterLevel,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new UpdateExpressionParameter(
                        e.ParameterId,
                        e.ParameterName,
                        e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new UpdatePackageStepCommand
        {
            StepId = step.StepId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            ConnectionId = step.ConnectionId,
            PackageFolderName = step.PackageFolderName,
            PackageProjectName = step.PackageProjectName,
            PackageName = step.PackageName,
            ExecuteIn32BitMode = step.ExecuteIn32BitMode,
            ExecuteAsLogin = step.ExecuteAsLogin,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

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
