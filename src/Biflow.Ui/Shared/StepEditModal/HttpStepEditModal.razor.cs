namespace Biflow.Ui.Shared.StepEditModal;

public partial class HttpStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<HttpStep>(mediator, toaster, dbContextFactory)
{
    internal override string FormId => "http_step_edit_form";
    
    private const string DisableAsyncPatternInfoContent = """
        <div>
            Option to disable invoking HTTP GET on location given in response header of a HTTP 202 Response.
            If set true, it stops invoking HTTP GET on http location given in response header.
            If set false then continues to invoke HTTP GET call on location given in http response headers.
        </div>
        """;

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the URL, header values and request body.</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@ModifiedSince</code> with a value of <code>2024-09-01</code>.
                A request body like <code>{ "ModifiedSince": "@ModifiedSince" }</code> will become <code>{ "ModifiedSince": "2024-09-01" }</code>
            </p>
        </div>
        """;
    
    private CodeEditor? _editor;
    
    protected override Task<HttpStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.HttpSteps
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

    protected override HttpStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0
        };
    
    protected override async Task<HttpStep> OnSubmitCreateAsync(HttpStep step)
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
        var command = new CreateHttpStepCommand
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
            Url = step.Url,
            Method = step.Method,
            Body = step.Body,
            BodyFormat = step.BodyFormat,
            Headers = step.Headers.ToArray(),
            DisableAsyncPattern = step.DisableAsyncPattern,
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

    protected override async Task<HttpStep> OnSubmitUpdateAsync(HttpStep step)
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
        var command = new UpdateHttpStepCommand
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
            Url = step.Url,
            Method = step.Method,
            Body = step.Body,
            BodyFormat = step.BodyFormat,
            Headers = step.Headers.ToArray(),
            DisableAsyncPattern = step.DisableAsyncPattern,
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
    
    protected override async Task OnModalShownAsync(HttpStep step)
    {
        if (_editor is not null)
        {
            try
            {
                await _editor.SetValueAsync(step.Body);
            }
            catch { /* ignored */ }
        }
    }

    private Task SetLanguageAsync(HttpBodyFormat language)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.BodyFormat = language;
        ArgumentNullException.ThrowIfNull(_editor);
        var inputLanguage = language switch
        {
            HttpBodyFormat.Json => "json",
            _ => ""
        };
        return _editor.SetLanguageAsync(inputLanguage);
    }
    
}
