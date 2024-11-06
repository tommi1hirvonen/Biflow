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

    private List<Dependency>? dependenciesToAdd;
    private List<Dependency>? dependenciesToRemove;

    private async Task CalculateChangesAsync()
    {
        if (Job is null) return;
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(step => step.JobId == Job.JobId)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.Dependencies)
            .ThenInclude(dep => dep.DependantOnStep)
            .ToListAsync();

        dependenciesToAdd = [];
        dependenciesToRemove = [];

        foreach (var step in steps)
        {
            // Check for missing dependencies based on sources and targets.
            var sources = step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Source);
            static IEnumerable<StepDataObject> targetsOf(Step step) => step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Target);

            var dependencies = steps.Where(s =>
                targetsOf(s).Any(target =>
                    sources.Any(source => source.IsSubsetOf(target))));
            var missingDependencies = dependencies.Where(s => !step.Dependencies.Any(d => s.StepId == d.DependantOnStepId));
            foreach (var missing in missingDependencies)
            {
                var dependency = new Dependency
                {
                    StepId = step.StepId,
                    Step = step,
                    DependantOnStepId = missing.StepId,
                    DependantOnStep = missing,
                };
                dependenciesToAdd.Add(dependency);
            }

            // Check for unnecessary dependencies based on sources and targets.
            // Only do this if there are any sources listed.
            if (sources.Any())
            {
                var extraDependencies = step.Dependencies.Where(d => !dependencies.Any(dep => d.DependantOnStepId == dep.StepId));
                dependenciesToRemove.AddRange(extraDependencies);
            }
        }
    }

    private async Task CommitAllAsync()
    {
        try
        {
            while (dependenciesToAdd?.Count > 0)
            {
                await AddDependencyAsync(dependenciesToAdd.First());
            }
            while (dependenciesToRemove?.Count > 0)
            {
                await RemoveDependencyAsync(dependenciesToRemove.First());
            }
            dependenciesToAdd = null;
            dependenciesToRemove = null;
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
        await _mediator.SendAsync(new CreateDependencyCommand(dependency));

        // Add dependency to the step loaded into memory.
        var step = Steps?.FirstOrDefault(step => step.StepId == dependency.StepId);
        var dependant = Steps?.FirstOrDefault(step => step.StepId == dependency.DependantOnStepId);
        if (step is not null && dependant is not null)
        {
            dependency.Step = step;
            dependency.DependantOnStep = dependant;
            step.Dependencies.Add(dependency);
        }

        dependenciesToAdd?.Remove(dependency);
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
        dependenciesToRemove?.Remove(dependency);
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
