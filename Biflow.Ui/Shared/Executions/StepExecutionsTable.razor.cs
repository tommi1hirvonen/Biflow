using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.Executions;

public partial class StepExecutionsTable : ComponentBase
{
    [Inject] private ToasterService Toaster { get; set; } = null!;
    
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;
    
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;
    
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    
    [Inject] private IMediator Mediator { get; set; } = null!;
    
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter]
    public IEnumerable<StepExecutionProjection>? Executions { get; set; }

    [Parameter]
    public bool ShowDetailed { get; set; } = true;

    [Parameter]
    public Func<StepExecutionProjection, StepExecutionAttempt?>? DetailStepProvider { get; set; }

    [Parameter]
    public EventCallback OnStepsUpdated { get; set; }

    [Parameter]
    public EventCallback<StepExecutionSortMode> OnSortingChanged { get; set; }

    [Parameter]
    public StepExecutionSortMode SortMode { get; set; }

    private readonly HashSet<StepExecutionId> selectedSteps = [];

    private StepExecutionProjection? selectedStepExecution;
    private StepHistoryOffcanvas? stepHistoryOffcanvas;
    private StepExecutionAttempt? detailStep;

    private record StepExecutionId(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);

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

    private async Task ToggleSelectedStepExecutionAsync(StepExecutionProjection execution)
    {
        // If the selected execution is the same that was previously selected, set to null
        // => hides step execution details component.
        if (selectedStepExecution == execution)
        {
            selectedStepExecution = null;
            detailStep = null;
        }
        else
        {
            selectedStepExecution = execution;

            if (DetailStepProvider is not null)
            {
                detailStep = DetailStepProvider(execution);
            }
            else
            {
                using var context = await DbContextFactory.CreateDbContextAsync();
                detailStep = await context.StepExecutionAttempts
                    .AsNoTrackingWithIdentityResolution()
                    .Include($"{nameof(StepExecutionAttempt.StepExecution)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
                    .Include($"{nameof(StepExecutionAttempt.StepExecution)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                    .Include(e => e.StepExecution)
                    .ThenInclude(e => e.Execution)
                    .ThenInclude(e => e.ExecutionParameters)
                    .Include(e => e.StepExecution)
                    .ThenInclude(e => e.ExecutionConditionParameters)
                    .ThenInclude(e => e.ExecutionParameter)
                    .Include(e => e.StepExecution)
                    .FirstOrDefaultAsync(e => e.ExecutionId == execution.ExecutionId && e.StepId == execution.StepId && e.RetryAttemptIndex == execution.RetryAttemptIndex);
                if (detailStep is not null)
                {
                    var step = await context.Steps.FirstOrDefaultAsync(s => s.StepId == detailStep.StepId);
                    detailStep.StepExecution.SetStep(step);
                }
            }
        }
        StateHasChanged();
    }

    private async Task StopStepExecutionAsync(Guid executionId, Guid stepId, string stepName)
    {
        if (!await Confirmer.ConfirmAsync("Stop step execution", $"Are you sure you want to stop \"{stepName}\"?"))
        {
            return;
        }
        try
        {
            ArgumentNullException.ThrowIfNull(AuthenticationState);
            var authState = await AuthenticationState;
            var username = authState.User.Identity?.Name;
            ArgumentNullException.ThrowIfNull(username);

            await ExecutorService.StopExecutionAsync(executionId, stepId, username);
            Toaster.AddSuccess("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            Toaster.AddError("Operation timed out", "The executor process may no longer be running");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error stopping execution", ex.Message);
        }
    }

    private void ToggleStepSelected(StepExecutionProjection step)
    {
        var id = new StepExecutionId(step.ExecutionId, step.StepId, step.RetryAttemptIndex);
        if (!selectedSteps.Remove(id))
        {
            selectedSteps.Add(id);
        }
    }

    private void ToggleStepsSelected(bool value)
    {
        if (value)
        {
            var stepsToAdd = Executions?
                .Select(s => new StepExecutionId(s.ExecutionId, s.StepId, s.RetryAttemptIndex))
                .Where(s => !selectedSteps.Contains(s))
                ?? [];
            foreach (var s in stepsToAdd) selectedSteps.Add(s);
        }
        else
        {
            selectedSteps.Clear();
        }
    }

    private async Task StopSelectedStepsAsync()
    {
        if (!await Confirmer.ConfirmAsync("Stop steps", $"Stop selected {selectedSteps.Count} step(s)?"))
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

            var executions = selectedSteps
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
                            steps, s => s.StepId, s => s.Dependencies);
                        steps = steps.OrderByDescending(s => s, comparer);
                    }
                    catch (Exception ex)
                    {
                        steps = steps.OrderByDescending(s => s.ExecutionPhase);
                        Toaster.AddWarning("Error sorting steps", $"Steps could not be sorted for optimal cancellation: {ex.Message}");
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
                        await ExecutorService.StopExecutionAsync(executionId, stepId, username);
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
                await JS.InvokeVoidAsync("console.error", error);
            }

            var errorMessage = distinctErrors.Length == 1
                ? distinctErrors[0]
                : "See browser console for detailed errors";

            if (successCount > 0 && distinctErrors.Length > 0)
            {
                Toaster.AddWarning("Error canceling some steps", errorMessage);
            }
            else if (distinctErrors.Length > 0)
            {
                Toaster.AddError("Error canceling steps", errorMessage);
            }
            else
            {
                Toaster.AddSuccess("Cancellations requested successfully");
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error canceling steps", ex.Message);
        }
    }

    private async Task UpdateExecutionStatusAsync(StepExecutionStatus status)
    {
        try
        {
            foreach (var step in selectedSteps)
            {
                var command = new UpdateStepExecutionAttemptStatusCommand(step.ExecutionId, step.StepId, step.RetryAttemptIndex, status);
                await Mediator.SendAsync(command);
            }
            Toaster.AddSuccess("Statuses updated successfully");
            await OnStepsUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error updating statuses", ex.Message);
        }
    }
}
