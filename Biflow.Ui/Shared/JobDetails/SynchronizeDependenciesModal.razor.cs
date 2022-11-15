using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.JobDetails;

public partial class SynchronizeDependenciesModal : ComponentBase
{
    [Inject] public IDbContextFactory<BiflowContext> DbContextFactory { get; set; } = null!;
    [Inject] public MarkupHelperService MarkupHelper { get; set; } = null!;

    [Parameter] public Job? Job { get; set; }

    [Parameter] public IEnumerable<Step>? Steps { get; set; }

    [Parameter] public Action? OnModalClosed { get; set; }

    private HxModal? Modal { get; set; }

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
            .ToListAsync();

        DependenciesToAdd = new();
        DependenciesToRemove = new();

        foreach (var step in steps)
        {
            // Check for missing dependencies based on sources and targets.
            var dependencies = steps.Where(s => s.Targets.Any(target => step.Sources.Any(source => source.Equals(target))));
            var missingDependencies = dependencies.Where(s => !step.Dependencies.Any(d => s.StepId == d.DependantOnStepId));
            foreach (var missing in missingDependencies)
            {
                var dependency = new Dependency
                {
                    StepId = step.StepId,
                    Step = step,
                    DependantOnStepId = missing.StepId,
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
        while (DependenciesToAdd?.Any() ?? false)
        {
            await AddDependencyAsync(DependenciesToAdd.First());
        }
        while (DependenciesToRemove?.Any() ?? false)
        {
            await RemoveDependencyAsync(DependenciesToRemove.First());
        }
        await Modal.LetAsync(x => x.HideAsync());
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

    private void OnClosed()
    {
        DependenciesToAdd = null;
        DependenciesToRemove = null;
        OnModalClosed?.Invoke();
    }

    public Task ShowAsync() => Modal.LetAsync(x => x.ShowAsync());
}
