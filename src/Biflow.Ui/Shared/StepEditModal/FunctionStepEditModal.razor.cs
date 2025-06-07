using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class FunctionStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<FunctionStep>(mediator, toaster, dbContextFactory)
{
    internal override string FormId => "function_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the function input (request body).</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@ModifiedSince</code> with a value of <code>2024-09-01</code>.
                A request body like <code>{ "ModifiedSince": "@ModifiedSince" }</code> will become <code>{ "ModifiedSince": "2024-09-01" }</code>
            </p>
        </div>
        """;

    private FunctionSelectOffcanvas? _functionSelectOffcanvas;
    private CodeEditor? _editor;
    
    protected override Task<FunctionStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.FunctionSteps
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

    protected override FunctionStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0
        };
    
    protected override async Task<FunctionStep> OnSubmitCreateAsync(FunctionStep step)
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
        var command = new CreateFunctionStepCommand
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
            FunctionAppId = step.FunctionAppId,
            FunctionUrl = step.FunctionUrl,
            FunctionInput = step.FunctionInput,
            FunctionInputFormat = step.FunctionInputFormat,
            FunctionIsDurable = step.FunctionIsDurable,
            FunctionKey = step.FunctionKey,
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

    protected override async Task<FunctionStep> OnSubmitUpdateAsync(FunctionStep step)
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
        var command = new UpdateFunctionStepCommand
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
            FunctionAppId = step.FunctionAppId,
            FunctionUrl = step.FunctionUrl,
            FunctionInput = step.FunctionInput,
            FunctionInputFormat = step.FunctionInputFormat,
            FunctionIsDurable = step.FunctionIsDurable,
            FunctionKey = step.FunctionKey,
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
    
    protected override async Task OnModalShownAsync(FunctionStep step)
    {
        if (_editor is not null)
        {
            try
            {
                await _editor.SetValueAsync(step.FunctionInput);
            }
            catch { /* ignored */ }
        }
    }

    private Task OpenFunctionSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return Step.FunctionAppId is { } id
            ? _functionSelectOffcanvas.LetAsync(x => x.ShowAsync(id))
            : Task.CompletedTask;
    }

    private void OnFunctionSelected(string functionUrl)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.FunctionUrl = functionUrl;
    }

    private Task SetLanguageAsync(FunctionInputFormat language)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.FunctionInputFormat = language;
        ArgumentNullException.ThrowIfNull(_editor);
        var inputLanguage = language switch
        {
            FunctionInputFormat.Json => "json",
            _ => ""
        };
        return _editor.SetLanguageAsync(inputLanguage);
    }
    
}
