using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class ExeStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IExecutorService executorService,
    ProxyClientFactory proxyClientFactory,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<ExeStep>(mediator, toaster, dbContextFactory)
{
    internal override string FormId => "exe_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the executable arguments property.</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@param1</code> with a value of <code>https://example.com</code>.
                An arguments property like <code>--url @param1</code> will become <code>--url https://example.com</code>
            </p>
        </div>
        """;
    
    private FileExplorerOffcanvas? _fileExplorerOffcanvas;

    protected override ExeStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0
        };

    protected override Task<ExeStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.ExeSteps
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
    
    protected override async Task<ExeStep> OnSubmitCreateAsync(ExeStep step)
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
            .Select(p => new CreateStepParameter(
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new CreateExeStepCommand
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
            FilePath = step.ExeFileName ?? "",
            Arguments = step.ExeArguments,
            WorkingDirectory = step.ExeWorkingDirectory,
            SuccessExitCode = step.ExeSuccessExitCode,
            RunAsCredentialId = step.RunAsCredentialId,
            ProxyId = step.ProxyId,
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

    protected override async Task<ExeStep> OnSubmitUpdateAsync(ExeStep step)
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
            .Select(p => new UpdateStepParameter(
                p.ParameterId,
                p.ParameterName,
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
        var command = new UpdateExeStepCommand
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
            FilePath = step.ExeFileName ?? "",
            Arguments = step.ExeArguments,
            WorkingDirectory = step.ExeWorkingDirectory,
            SuccessExitCode = step.ExeSuccessExitCode,
            RunAsCredentialId = step.RunAsCredentialId,
            ProxyId = step.ProxyId,
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
    
    private Task OpenFileSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        FileExplorerDelegate fileExplorerDelegate;
        if (Step.ProxyId is { } id)
        {
            var proxy = Integrations.Proxies.First(x => x.ProxyId == id);
            var client = proxyClientFactory.Create(proxy);
            fileExplorerDelegate = client.GetDirectoryItemsAsync;
        }
        else
        {
            fileExplorerDelegate = executorService.GetDirectoryItemsAsync;
        }
        return _fileExplorerOffcanvas.LetAsync(x => x.ShowAsync(fileExplorerDelegate));
    }
    
    private void OnFileSelected(string filePath)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.ExeFileName = filePath;
    }
}
