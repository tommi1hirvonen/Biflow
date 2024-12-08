using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PipelineStepEditModal(
    ITokenService tokenService,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<PipelineStep>(toaster, dbContextFactory)
{
    [Parameter] public IList<PipelineClient> PipelineClients { get; set; } = [];

    private readonly ITokenService _tokenService = tokenService;

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
            var pipelineClient = client.CreatePipelineClient(_tokenService);
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
        ArgumentNullException.ThrowIfNull(Step?.PipelineClientId);
        return _pipelineSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.PipelineClientId));
    }

    private void OnPipelineSelected(string pipelineName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PipelineName = pipelineName;
    }
}
