using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;

namespace Biflow.Ui.Shared.JobDetails;

public partial class JobParameters(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    JobValidator jobValidator,
    IMediator mediator) : ComponentBase
{
    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly JobValidator _jobValidator = jobValidator;
    private readonly IMediator _mediator = mediator;

    private EditContext? editContext;
    private Job? editJob;
    private bool hasChanges = false;
    private bool loading = false;
    private FluentValidationValidator? fluentJobValidator;
    private ExpressionEditOffcanvas<JobParameter>? expressionEditOffcanvas;
    private HxOffcanvas? referencingStepsOffcanvas;
    private ReferencingStepsModel referencingSteps = new(new(), [], [], [], []);

    protected override async Task OnParametersSetAsync()
    {
        if (Job is null || loading || Job.JobId == editJob?.JobId)
        {
            return;
        }
        loading = true;
        using var context = _dbContextFactory.CreateDbContext();
        editJob = await context.Jobs
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.InheritingStepParameters)
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.AssigningStepParameters)
            .ThenInclude(p => p.Step) // Assigning steps are from other jobs, which means they are not in the Steps List property
            .ThenInclude(s => s.Job)
            .FirstAsync(j => j.JobId == Job.JobId);
        editJob.JobParameters.SortBy(x => x.ParameterName);
        loading = false;
        editContext = new(editJob);
        editContext.OnFieldChanged += (sender, args) => hasChanges = true;
    }

    private void AddParameter() => editJob?.JobParameters
        .Insert(0, new JobParameter());

    private async Task SubmitParameters()
    {
        ArgumentNullException.ThrowIfNull(editJob);
        foreach (var param in editJob.JobParameters)
        {
            // Update the referencing job step parameter names to match the possibly changed new name.
            foreach (var referencingJobStepParam in param.AssigningStepParameters)
            {
                referencingJobStepParam.ParameterName = param.ParameterName;
            }
        }

        try
        {
            await _mediator.SendAsync(new UpdateJobParametersCommand(editJob));
            hasChanges = false;
            _toaster.AddSuccess("Job parameters updated successfully");
        }
        catch (DbUpdateConcurrencyException)
        {
            _toaster.AddError("Concurrency error",
                "The job has been modified outside of this session. Reload the page to view the most recent settings.");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error saving parameters", $"{ex.Message}\n{ex.InnerException?.Message}");
        }

    }

    private async Task RemoveParameter(JobParameter parameter)
    {
        var referencingSteps = GetInheritingSteps(parameter).Concat(GetCapturingSteps(parameter)).Concat(GetAssigningSteps(parameter));
        if (referencingSteps.Any())
        {
            var confirmResult = await _confirmer.ConfirmAsync("This parameter has one or more referencing steps. Removing it can break these steps. Delete anyway?");
            if (!confirmResult)
            {
                return;
            }
        }

        editJob?.JobParameters.Remove(parameter);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (!hasChanges)
        {
            return;
        }

        var confirmed = await _confirmer.ConfirmAsync("", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private async Task ShowReferencingStepsAsync(JobParameter param)
    {
        referencingSteps = new(param, GetInheritingSteps(param), GetCapturingSteps(param), GetAssigningSteps(param), GetExecutionConditionSteps(param));
        await referencingStepsOffcanvas.LetAsync(x => x.ShowAsync());
    }

    private IEnumerable<Step> GetInheritingSteps(JobParameter parameter) => Steps
        ?.Where(s => s is IHasStepParameters hasParams &&
            hasParams.StepParameters.Any(p => p.InheritFromJobParameterId == parameter.ParameterId || p.ExpressionParameters.Any(ep => ep.InheritFromJobParameterId == parameter.ParameterId)))
        .OrderBy(s => s.StepName)
        .AsEnumerable()
        ?? [];

    private IEnumerable<Step> GetCapturingSteps(JobParameter parameter) => Steps
        ?.Where(s => s is SqlStep sql && sql.ResultCaptureJobParameterId == parameter.ParameterId)
        .OrderBy(s => s.StepName)
        .AsEnumerable()
        ?? [];

    private static IEnumerable<Step> GetAssigningSteps(JobParameter parameter) => parameter.AssigningStepParameters
        .Select(p => p.Step)
        .OrderBy(s => s.Job.JobName)
        .ThenBy(s => s.StepName)
        .AsEnumerable()
        ?? [];

    private IEnumerable<Step> GetExecutionConditionSteps(JobParameter parameter) => Steps
        ?.Where(s => s.ExecutionConditionParameters.Any(p => p.JobParameterId == parameter.ParameterId))
        ?? [];

    private record ReferencingStepsModel(
        JobParameter Parameter,
        IEnumerable<Step> InheritingSteps,
        IEnumerable<Step> CapturingSteps,
        IEnumerable<Step> AssigningSteps,
        IEnumerable<Step> ExecutionConditionSteps);

}