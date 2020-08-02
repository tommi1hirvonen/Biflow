using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;
using System.Collections;

namespace EtlManager.Pages.Jobs.Steps
{
    public class EditModel : PageModel
    {
        private readonly EtlManager.Data.EtlManagerContext _context;

        public EditModel(EtlManager.Data.EtlManagerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Step Step { get; set; }

        public Job Job { get; set; }

        [BindProperty]
        public IList<DependencyEdit> DependencyEdits { get; set; } = new List<DependencyEdit>();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.Steps
                .Include(step => step.Job)
                .Include(step => step.Dependencies)
                .ThenInclude(dependency => dependency.DependantOnStep)
                .FirstOrDefaultAsync(m => m.StepId == id);

            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == Step.JobId);

            // Iterate through all the steps in this job except for the current step.
            foreach (var step in Job.Steps.Where(step_ => step_.StepId != Step.StepId))
            {
                // If the step's current dependencies include the step we are at, add a new DependencyEdit with Enabled = true
                if (Step.Dependencies.Select(step => step.DependantOnStepId).Contains(step.StepId))
                {
                    var dependency = Step.Dependencies.First(d => d.DependantOnStepId == step.StepId);
                    DependencyEdits.Add(new DependencyEdit
                    {
                        DependencyId = dependency.DependencyId,
                        StepId = dependency.StepId,
                        Step = dependency.Step,
                        DependantOnStepId = dependency.DependantOnStepId,
                        DependantOnStep = dependency.DependantOnStep,
                        StrictDependency = dependency.StrictDependency,
                        Enabled = true
                    });
                }
                // Otherwise add a new DependencyEdit with enabled = false.
                else
                {
                    DependencyEdits.Add(new DependencyEdit {
                        StepId = Step.StepId,
                        Step = Step,
                        DependantOnStepId = step.StepId,
                        DependantOnStep = step,
                        StrictDependency = false,
                        Enabled = false
                    });
                }
            }

            DependencyEdits = DependencyEdits.OrderBy(d => d.DependantOnStep.StepName).ToList();

            if (Step == null)
            {
                return NotFound();
            }
            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // The JobId property of the Job member of Step is lost. Restore it before saving.
            // Otherwise Entity Framework will try to generate a new Job and place the step under it.
            Step.Job.JobId = Step.JobId;

            // Get all current dependencies for this step.
            IList<Dependency> dependencies = await _context.Dependencies.Where(d => d.StepId == Step.StepId).ToListAsync();

            // Iterate over all edited dependencies where Enabled = true
            foreach (var dependencyEdit in DependencyEdits.Where(d => d.Enabled == true))
            {
                var newDependency = (Dependency)dependencyEdit;

                // Get corresponding old dependency.
                var oldDependency = dependencies.FirstOrDefault(d => d.DependencyId == newDependency.DependencyId);

                // MODIFIED - both exist but properties differ
                if (oldDependency != null && newDependency.StrictDependency != oldDependency.StrictDependency)
                {
                    oldDependency.StrictDependency = newDependency.StrictDependency;
                }
                // NEW - old dependency doesn't exist
                else if (oldDependency == null)
                {
                    _context.Dependencies.Add(newDependency);
                }
            }

            // Iterate over all the old dependencies.
            foreach (var dependency in dependencies)
            {
                // REMOVED - the old dependency isn't among the edited ones where Enabled = true.
                if (!DependencyEdits.Where(d => d.Enabled == true).Select(d => d.DependencyId).Contains(dependency.DependencyId))
                {
                    _context.Dependencies.Remove(dependency);
                }
            }
            
            _context.Attach(Step).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StepExists(Step.StepId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index", new { id = Step.JobId});
        }

        private bool StepExists(Guid id)
        {
            return _context.Steps.Any(e => e.StepId == id);
        }
    }
}
