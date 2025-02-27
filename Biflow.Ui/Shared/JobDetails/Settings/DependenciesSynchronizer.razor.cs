using Microsoft.AspNetCore.Components.Routing;

namespace Biflow.Ui.Shared.JobDetails.Settings;

public partial class DependenciesSynchronizer(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IMediator mediator) : ComponentBase
{
    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;

    private List<Dependency>? _dependenciesToAdd;
    private List<Dependency>? _dependenciesToRemove;

    private async Task CalculateChangesAsync()
    {
        if (Job is null) return;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(step => step.JobId == Job.JobId)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.Dependencies)
            .ThenInclude(dep => dep.DependantOnStep)
            .ToListAsync();

        _dependenciesToAdd = [];
        _dependenciesToRemove = [];

        foreach (var step in steps)
        {
            // Check for missing dependencies based on sources and targets.
            var sources = step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Source);

            var dependencies = steps
                .Where(s =>
                    TargetsOf(s).Any(target => sources.Any(source => source.IsSubsetOf(target))))
                .ToArray();
            var missingDependencies = dependencies.Where(s => step.Dependencies.All(d => s.StepId != d.DependantOnStepId));
            foreach (var missing in missingDependencies)
            {
                var dependency = new Dependency
                {
                    StepId = step.StepId,
                    Step = step,
                    DependantOnStepId = missing.StepId,
                    DependantOnStep = missing,
                };
                _dependenciesToAdd.Add(dependency);
            }

            // Check for unnecessary dependencies based on sources and targets.
            // Only do this if there are any sources listed.
            if (sources.Any())
            {
                var extraDependencies = step.Dependencies
                    .Where(d => dependencies.All(dep => d.DependantOnStepId != dep.StepId));
                _dependenciesToRemove.AddRange(extraDependencies);
            }

            continue;

            static IEnumerable<StepDataObject> TargetsOf(Step step) =>
                step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Target);
        }
    }

    private async Task CommitAllAsync()
    {
        try
        {
            while (_dependenciesToAdd?.Count > 0)
            {
                await AddDependencyAsync(_dependenciesToAdd.First());
            }
            while (_dependenciesToRemove?.Count > 0)
            {
                await RemoveDependencyAsync(_dependenciesToRemove.First());
            }
            _dependenciesToAdd = null;
            _dependenciesToRemove = null;
            SortSteps?.Invoke();
            _toaster.AddSuccess("Changes saved successfully");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error saving changes", ex.Message);
        }
    }

    private async Task AddDependencyAsync(Dependency dependency)
    {
        // Add dependency to database.
        var command = new CreateDependencyCommand(
            dependency.StepId,
            dependency.DependantOnStepId,
            dependency.DependencyType);
        await _mediator.SendAsync(command);

        // Add dependency to the step loaded into memory.
        var step = Steps?.FirstOrDefault(s => s.StepId == dependency.StepId);
        var dependant = Steps?.FirstOrDefault(s => s.StepId == dependency.DependantOnStepId);
        if (step is not null && dependant is not null)
        {
            dependency.Step = step;
            dependency.DependantOnStep = dependant;
            step.Dependencies.Add(dependency);
        }

        _dependenciesToAdd?.Remove(dependency);
    }

    private async Task RemoveDependencyAsync(Dependency dependency)
    {
        // Remove dependency from the database.
        await _mediator.SendAsync(new DeleteDependencyCommand(dependency.StepId, dependency.DependantOnStepId));

        // Remove dependency from step loaded into memory.
        var step = Steps?.FirstOrDefault(step => step.StepId == dependency.StepId);
        var dep = step?.Dependencies.FirstOrDefault(d => d.DependantOnStepId == dependency.DependantOnStepId);
        if (dep is not null)
        {
            step?.Dependencies.Remove(dep);
        }

        // Dependency was handled => remove from list of modifications.
        _dependenciesToRemove?.Remove(dependency);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await _confirmer.ConfirmAsync("", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }
}
