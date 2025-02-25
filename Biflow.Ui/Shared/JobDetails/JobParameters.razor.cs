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

    private EditContext? _editContext;
    private Job? _editJob;
    private bool _hasChanges = false;
    private bool _loading = false;
    private FluentValidationValidator? _fluentJobValidator;
    private ExpressionEditOffcanvas<JobParameter>? _expressionEditOffcanvas;
    private HxOffcanvas? _referencingStepsOffcanvas;
    private ReferencingStepsModel _referencingSteps = new(new(), [], [], [], []);

    protected override async Task OnParametersSetAsync()
    {
        if (Job is null || _loading || Job.JobId == _editJob?.JobId)
        {
            return;
        }
        _loading = true;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        _editJob = await context.Jobs
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.InheritingStepParameters)
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.AssigningStepParameters)
            .ThenInclude(p => p.Step) // Assigning steps are from other jobs, which means they are not in the Steps List property
            .ThenInclude(s => s.Job)
            .FirstAsync(j => j.JobId == Job.JobId);
        _editJob.JobParameters.SortBy(x => x.ParameterName);
        _loading = false;
        _editContext = new(_editJob);
        _editContext.OnFieldChanged += (_, _) => _hasChanges = true;
    }

    private void AddParameter() => _editJob?.JobParameters
        .Insert(0, new JobParameter());

    private async Task SubmitParameters()
    {
        ArgumentNullException.ThrowIfNull(_editJob);
        foreach (var param in _editJob.JobParameters)
        {
            // Update the referencing job step parameter names to match the possibly changed new name.
            foreach (var referencingJobStepParam in param.AssigningStepParameters)
            {
                referencingJobStepParam.ParameterName = param.ParameterName;
            }
        }

        try
        {
            var parameters = _editJob.JobParameters
                .Select(x => new UpdateJobParameter(
                    x.ParameterId == Guid.Empty ? null : x.ParameterId,
                    x.ParameterName,
                    x.ParameterValue,
                    x.UseExpression,
                    x.Expression.Expression))
                .ToArray();
            var command = new UpdateJobParametersCommand(_editJob.JobId, parameters);
            var response = await _mediator.SendAsync(command);
            // Synchronize parameters so that newly added parameters are managed using their proper parameter ids
            // coming from the response. 
            foreach (var parameter in _editJob.JobParameters
                         .Where(p1 => response.All(p2 => p1.ParameterId != p2.ParameterId)).ToArray())
            {
                _editJob.JobParameters.Remove(parameter);
            }
            foreach (var parameter in response
                         .Where(p1 => _editJob.JobParameters.All(p2 => p1.ParameterId != p2.ParameterId)))
            {
                _editJob.JobParameters.Add(parameter);
            }
            _editJob.JobParameters.SortBy(p => p.ParameterName);
            _hasChanges = false;
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

        _editJob?.JobParameters.Remove(parameter);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (!_hasChanges)
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
        _referencingSteps = new(param, GetInheritingSteps(param), GetCapturingSteps(param), GetAssigningSteps(param), GetExecutionConditionSteps(param));
        await _referencingStepsOffcanvas.LetAsync(x => x.ShowAsync());
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
        .ThenBy(s => s.StepName);

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