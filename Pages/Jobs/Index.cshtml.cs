using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;


namespace EtlManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public IndexModel(EtlManagerContext context)
        {
            _context = context;
        }

        public IList<Job> Jobs { get;set; }

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.ToListAsync();
        }

        public async Task<IActionResult> OnPostCopy(Guid id)
        {
            Job clone = await _context.Jobs
                .Include(job => job.Schedules)
                .Include(job => job.Steps)
                .ThenInclude(step => step.Dependencies)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.JobId == id);

            if (clone == null)
            {
                return NotFound();
            }

            clone.JobId = Guid.NewGuid();
            Dictionary<Guid, Guid> idMapping = new Dictionary<Guid, Guid>();
            foreach (var step in clone.Steps)
            {
                var oldId = step.StepId;
                step.StepId = Guid.NewGuid();
                var newId = step.StepId;
                idMapping.Add(oldId, newId);
            }

            foreach (var schedule in clone.Schedules)
            {
                schedule.ScheduleId = Guid.NewGuid();
            }

            foreach (var step in clone.Steps)
            {
                foreach (var dependency in step.Dependencies)
                {
                    dependency.DependencyId = Guid.NewGuid();
                    dependency.StepId = idMapping[dependency.StepId];
                }
            }

            _context.Jobs.Add(clone);
            await _context.SaveChangesAsync();

            foreach (var step in clone.Steps)
            {
                foreach (var dependency in step.Dependencies)
                {
                    dependency.DependantOnStepId = idMapping[dependency.DependantOnStepId];
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

    }
}
