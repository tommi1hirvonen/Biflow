using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PipelineStepEditModal(
    ITokenService tokenService,
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<PipelineStep>(mediator, toaster, dbContextFactory)
{
    [Parameter] public IList<PipelineClient> PipelineClients { get; set; } = [];
    
    internal override string FormId => "pipeline_step_edit_form";

    private PipelineSelectOffcanvas? _pipelineSelectOffcanvas;

    protected override PipelineStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            PipelineClientId = PipelineClients.First().PipelineClientId
        };

    protected override Task<PipelineStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.PipelineSteps
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
    
    protected override async Task<PipelineStep> OnSubmitCreateAsync(PipelineStep step)
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
        var command = new CreatePipelineStepCommand
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
            PipelineClientId = step.PipelineClientId,
            PipelineName = step.PipelineName,
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

    protected override async Task<PipelineStep> OnSubmitUpdateAsync(PipelineStep step)
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
        var command = new UpdatePipelineStepCommand
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
            PipelineClientId = step.PipelineClientId,
            PipelineName = step.PipelineName,
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

    private async Task ImportParametersAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Step?.PipelineName))
            {
                Toaster.AddWarning("Pipeline name was empty");
                return;
            }

            await using var context = await DbContextFactory.CreateDbContextAsync();
            var client = await context.PipelineClients
                .AsNoTrackingWithIdentityResolution()
                .Include(c => c.AzureCredential)
                .FirstAsync(c => c.PipelineClientId == Step.PipelineClientId);
            var pipelineClient = client.CreatePipelineClient(tokenService);
            var parameters = await pipelineClient.GetPipelineParametersAsync(Step.PipelineName);
            if (!parameters.Any())
            {
                Toaster.AddInformation($"No parameters for pipeline {Step.PipelineName}");
                return;
            }
            Step.StepParameters.Clear();
            foreach (var (name, value) in parameters)
            {
                Step.StepParameters.Add(new PipelineStepParameter
                {
                    ParameterName = name,
                    ParameterValue = value
                });
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error importing parameters", ex.Message);
        }
    }

    private Task OpenPipelineSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _pipelineSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.PipelineClientId));
    }

    private void OnPipelineSelected(string pipelineName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PipelineName = pipelineName;
    }
}
