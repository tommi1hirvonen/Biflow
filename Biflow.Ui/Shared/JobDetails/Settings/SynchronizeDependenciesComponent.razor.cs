using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.JobDetails.Settings;

public partial class SynchronizeDependenciesComponent : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;

    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter(Name = "SortSteps")] public Action? SortSteps { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    private List<Dependency>? DependenciesToAdd { get; set; }
    
    private List<Dependency>? DependenciesToRemove { get; set; }

    private async Task CalculateChangesAsync()
    {
        if (Job is null) return;
        using var context = DbContextFactory.CreateDbContext();
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(step => step.JobId == Job.JobId)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.Dependencies)
            .ThenInclude(dep => dep.DependantOnStep)
            .ToListAsync();

        DependenciesToAdd = new();
        DependenciesToRemove = new();

        foreach (var step in steps)
        {
            // Check for missing dependencies based on sources and targets.
            var dependencies = steps.Where(s => s.Targets.Any(target => step.Sources.Any(source => source.NamesEqual(target))));
            var missingDependencies = dependencies.Where(s => !step.Dependencies.Any(d => s.StepId == d.DependantOnStepId));
            foreach (var missing in missingDependencies)
            {
                var dependency = new Dependency(step.StepId, missing.StepId)
                {
                    Step = step,
                    DependantOnStep = missing
                };
                DependenciesToAdd.Add(dependency);
            }

            // Check for unnecessary dependencies based on sources and targets.
            // Only do this if there are any sources listed.
            if (step.Sources.Any())
            {
                var extraDependencies = step.Dependencies.Where(d => !dependencies.Any(dep => d.DependantOnStepId == dep.StepId));
                DependenciesToRemove.AddRange(extraDependencies);
            }
        }
    }

    private async Task CommitAllAsync()
    {
        try
        {
            while (DependenciesToAdd?.Any() ?? false)
            {
                await AddDependencyAsync(DependenciesToAdd.First());
            }
            while (DependenciesToRemove?.Any() ?? false)
            {
                await RemoveDependencyAsync(DependenciesToRemove.First());
            }
            DependenciesToAdd = null;
            DependenciesToRemove = null;
            SortSteps?.Invoke();
            Messenger.AddInformation("Changes saved successfully");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error saving changes", ex.Message);
        }
    }

    private async Task AddDependencyAsync(Dependency dependency)
    {
        // Add dependency to database.
        using var context = DbContextFactory.CreateDbContext();
        var existing = await context.Dependencies
            .FirstOrDefaultAsync(d => d.StepId == dependency.StepId && d.DependantOnStepId == dependency.DependantOnStepId);
        if (existing is null)
        {
            dependency.Step = null!;
            dependency.DependantOnStep = null!;
            context.Dependencies.Add(dependency);
            await context.SaveChangesAsync();
        }

        // Add dependency to the step loaded into memory.
        var step = Steps?.FirstOrDefault(step => step.StepId == dependency.StepId);
        var dependant = Steps?.FirstOrDefault(step => step.StepId == dependency.DependantOnStepId);
        if (step is not null && dependant is not null)
        {
            dependency.Step = step;
            dependency.DependantOnStep = dependant;
            step.Dependencies.Add(dependency);
        }

        DependenciesToAdd?.Remove(dependency);
    }

    private async Task RemoveDependencyAsync(Dependency dependency)
    {
        // Remove dependency from the database.
        using var context = DbContextFactory.CreateDbContext();
        var existing = await context.Dependencies
            .FirstOrDefaultAsync(d => d.StepId == dependency.StepId && d.DependantOnStepId == dependency.DependantOnStepId);
        if (existing is not null)
        {
            context.Dependencies.Remove(existing);
            await context.SaveChangesAsync();
        }

        // Remove dependency from step loaded into memory.
        var step = Steps?.FirstOrDefault(step => step.StepId == dependency.StepId);
        var dep = step?.Dependencies.FirstOrDefault(d => d.DependantOnStepId == dependency.DependantOnStepId);
        if (dep is not null)
        {
            step?.Dependencies.Remove(dep);
        }

        // Dependency was handled => remove from list of modifications.
        DependenciesToRemove?.Remove(dependency);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        var confirmed = await Confirmer.ConfirmAsync("", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }
}
