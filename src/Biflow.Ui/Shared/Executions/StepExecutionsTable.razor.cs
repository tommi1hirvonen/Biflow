using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.Executions;

public partial class StepExecutionsTable(
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IExecutorService executorService,
    IDbContextFactory<AppDbContext> dbContextFactory,
    NavigationManager navigationManager,
    IMediator mediator,
    IJSRuntime js) : ComponentBase
{
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; }

    [CascadingParameter] public StepExecutionMonitorsOffcanvas? StepExecutionMonitorsOffcanvas { get; set; }

    [Parameter] public IEnumerable<IStepExecutionProjection>? Executions { get; set; }

    [Parameter] public bool ShowDetailed { get; set; } = true;
    
    [Parameter] public bool ShowStepTags { get; set; }

    [Parameter] public Func<IStepExecutionProjection, StepExecutionAttempt?>? DetailStepProvider { get; set; }

    [Parameter] public EventCallback OnStepsUpdated { get; set; }

    [Parameter] public EventCallback<StepExecutionSortMode> OnSortingChanged { get; set; }

    [Parameter] public StepExecutionSortMode SortMode { get; set; }

    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IExecutorService _executorService = executorService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly NavigationManager _navigationManager = navigationManager;
    private readonly IMediator _mediator = mediator;
    private readonly IJSRuntime _js = js;
    private readonly HashSet<StepExecutionId> _selectedSteps = [];

    private IStepExecutionProjection? _selectedStepExecution;
    private StepHistoryOffcanvas? _stepHistoryOffcanvas;
    private StepExecutionAttempt? _detailStep;

    private record StepExecutionId(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);

    public Task RefreshSelectedStepExecutionAsync() =>
        _selectedStepExecution is not null ? LoadStepExecutionAsync(_selectedStepExecution) : Task.CompletedTask;

    private void ToggleJobSortMode()
    {
        var sortMode = SortMode switch
        {
            StepExecutionSortMode.JobAsc => StepExecutionSortMode.JobDesc,
            StepExecutionSortMode.JobDesc => StepExecutionSortMode.CreatedDesc,
            _ => StepExecutionSortMode.JobAsc
        };
        OnSortingChanged.InvokeAsync(sortMode);
    }

    private void ToggleStepSortMode()
    {
        var sortMode = SortMode switch
        {
            StepExecutionSortMode.StepAsc => StepExecutionSortMode.StepDesc,
            StepExecutionSortMode.StepDesc => StepExecutionSortMode.CreatedDesc,
            _ => StepExecutionSortMode.StepAsc
        };
        OnSortingChanged.InvokeAsync(sortMode);
    }

    private void ToggleStartedSortMode()
    {
        var sortMode = SortMode switch
        {
            StepExecutionSortMode.StartedAsc => StepExecutionSortMode.StartedDesc,
            StepExecutionSortMode.StartedDesc => StepExecutionSortMode.CreatedDesc,
            _ => StepExecutionSortMode.StartedAsc
        };
        OnSortingChanged.InvokeAsync(sortMode);
    }

    private void ToggleEndedSortMode()
    {
        var sortMode = SortMode switch
        {
            StepExecutionSortMode.EndedAsc => StepExecutionSortMode.EndedDesc,
            StepExecutionSortMode.EndedDesc => StepExecutionSortMode.CreatedDesc,
            _ => StepExecutionSortMode.EndedAsc
        };
        OnSortingChanged.InvokeAsync(sortMode);
    }

    private void ToggleDurationSortMode()
    {
        var sortMode = SortMode switch
        {
            StepExecutionSortMode.DurationAsc => StepExecutionSortMode.DurationDesc,
            StepExecutionSortMode.DurationDesc => StepExecutionSortMode.CreatedDesc,
            _ => StepExecutionSortMode.DurationAsc
        };
        OnSortingChanged.InvokeAsync(sortMode);
    }

    private async Task ToggleSelectedStepExecutionAsync(IStepExecutionProjection execution)
    {
        // If the selected execution is the same that was previously selected, set to null
        // => hides the step execution details component.
        if (_selectedStepExecution == execution)
        {
            _selectedStepExecution = null;
            _detailStep = null;
        }
        else
        {
            _selectedStepExecution = execution;
            await LoadStepExecutionAsync(execution);
        }
        StateHasChanged();
    }

    private async Task LoadStepExecutionAsync(IStepExecutionProjection execution)
    {
        if (DetailStepProvider is not null)
        {
            _detailStep = DetailStepProvider(execution);
        }
        else
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            _detailStep = await context.StepExecutionAttempts
                .Include(e => e.StepExecution)
                .ThenInclude(e => e.Execution) // Required for StepExecutionAttempt.CanBeStopped
                .FirstOrDefaultAsync(e => e.ExecutionId == execution.ExecutionId &&
                                          e.StepId == execution.StepId &&
                                          e.RetryAttemptIndex == execution.RetryAttemptIndex);
            // Request rendering for the data we already have.
            await InvokeAsync(StateHasChanged);
            // The rest of the data is loaded in the background, as it may take some time.
            // Use EF change tracking to populate the navigation properties of the previously loaded entity. 
            _ = await context.StepExecutions
                .Include(
                    $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
                .Include(
                    $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .Include(e => e.ExecutionConditionParameters)
                .ThenInclude(e => e.ExecutionParameter)
                .Include(e => ((SqlStepExecution)e).ResultCaptureJobParameter)
                .FirstOrDefaultAsync(e => e.ExecutionId == execution.ExecutionId && e.StepId == execution.StepId);
            await InvokeAsync(StateHasChanged);
            if (_detailStep is not null)
            {
                var step = await context.Steps.FirstOrDefaultAsync(s => s.StepId == _detailStep.StepId);
                _detailStep.StepExecution.SetStep(step);
            }
        }
    }

    private async Task StopStepExecutionAsync(Guid executionId, Guid stepId, string stepName)
    {
        if (!await _confirmer.ConfirmAsync("Stop step execution", $"Are you sure you want to stop \"{stepName}\"?"))
        {
            return;
        }
        try
        {
            ArgumentNullException.ThrowIfNull(AuthenticationState);
            var authState = await AuthenticationState;
            var username = authState.User.Identity?.Name;
            ArgumentNullException.ThrowIfNull(username);

            await _executorService.StopExecutionAsync(executionId, stepId, username);
            _toaster.AddSuccess("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            _toaster.AddError("Operation timed out", "The executor process may no longer be running");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error stopping execution", ex.Message);
        }
    }

    private void ToggleStepSelected(IStepExecutionProjection step)
    {
        var id = new StepExecutionId(step.ExecutionId, step.StepId, step.RetryAttemptIndex);
        if (!_selectedSteps.Remove(id))
        {
            _selectedSteps.Add(id);
        }
    }

    private void ToggleStepsSelected(bool value)
    {
        if (value)
        {
            var stepsToAdd = Executions?
                .Select(s => new StepExecutionId(s.ExecutionId, s.StepId, s.RetryAttemptIndex))
                .Where(s => !_selectedSteps.Contains(s))
                ?? [];
            foreach (var s in stepsToAdd) _selectedSteps.Add(s);
        }
        else
        {
            _selectedSteps.Clear();
        }
    }

    private async Task StopSelectedStepsAsync()
    {
        if (!await _confirmer.ConfirmAsync("Stop steps", $"Stop selected {_selectedSteps.Count} step(s)?"))
        {
            return;
        }

        try
        {
            ArgumentNullException.ThrowIfNull(Executions);
            ArgumentNullException.ThrowIfNull(AuthenticationState);
            var authState = await AuthenticationState;
            var username = authState.User.Identity?.Name;
            ArgumentNullException.ThrowIfNull(username);

            var executions = _selectedSteps
                .Select(s =>
                {
                    var step = Executions.First(e => e.ExecutionId == s.ExecutionId && e.StepId == s.StepId);
                    return (step.ExecutionId, step.ExecutionMode, step.StepId, step.ExecutionPhase, step.Dependencies, step.CanBeStopped);
                })
                .Where(s => s.CanBeStopped)
                .GroupBy(e => (e.ExecutionId, e.ExecutionMode), e => (e.ExecutionId, e.StepId, e.ExecutionPhase, e.Dependencies));

            var successCount = 0;
            var errorMessages = new List<string>();
            foreach (var group in executions)
            {
                var execution = group.Key;
                var steps = group.AsEnumerable();
                if (execution.ExecutionMode == ExecutionMode.Dependency)
                {
                    // When executing in dependency mode, start cancellation from the last steps to be executed.
                    // This way we should see as few DependenciesFailed statuses after cancellation as possible.
                    // The executor may be quick to mark depending steps as failed if we start canceling steps in ascending order.
                    try
                    {
                        var comparer = new TopologicalComparer<(Guid, Guid StepId, int, Guid[] Dependencies), Guid>(
                            steps,
                            s => s.StepId, s => s.Dependencies);
                        steps = steps.OrderByDescending(s => s, comparer);
                    }
                    catch (Exception ex)
                    {
                        steps = steps.OrderByDescending(s => s.ExecutionPhase);
                        _toaster.AddWarning("Error sorting steps", $"Steps could not be sorted for optimal cancellation: {ex.Message}");
                    }
                }
                else
                {
                    steps = steps.OrderByDescending(s => s.ExecutionPhase);
                }

                foreach (var (executionId, stepId, _, _) in steps)
                {
                    try
                    {
                        await _executorService.StopExecutionAsync(executionId, stepId, username);
                        successCount++;
                    }
                    catch (TimeoutException ex)
                    {
                        errorMessages.Add(ex.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add(ex.Message);
                    }
                }
            }

            var distinctErrors = errorMessages.Distinct().ToArray();
            foreach (var error in distinctErrors)
            {
                await _js.InvokeVoidAsync("console.error", error);
            }

            var errorMessage = distinctErrors.Length == 1
                ? distinctErrors[0]
                : "See browser console for detailed errors";

            if (successCount > 0 && distinctErrors.Length > 0)
            {
                _toaster.AddWarning("Error canceling some steps", errorMessage);
            }
            else if (distinctErrors.Length > 0)
            {
                _toaster.AddError("Error canceling steps", errorMessage);
            }
            else
            {
                _toaster.AddSuccess("Cancellations requested successfully");
            }
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error canceling steps", ex.Message);
        }
    }

    private async Task UpdateExecutionStatusAsync(StepExecutionStatus status)
    {
        try
        {
            var commands = from step in _selectedSteps
                           select new UpdateStepExecutionAttemptStatusCommand(
                               step.ExecutionId,
                               step.StepId,
                               step.RetryAttemptIndex, 
                               status);
            foreach (var command in commands)
            {
                await _mediator.SendAsync(command);
            }
            _toaster.AddSuccess("Statuses updated successfully");
            await OnStepsUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error updating statuses", ex.Message);
        }
    }
}
