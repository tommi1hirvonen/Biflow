using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs.JobDetails
{
    public class DependenciesModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly IAuthorizationService _authorizationService;

        public DependenciesModel(EtlManagerContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public Job Job { get; set; }

        public IList<Job> Jobs { get; set; }

        public IList<Step> Steps { get; set; }

        public IList<Dependency> Dependencies { get; set; }

        [BindProperty]
        public string NewJobName { get; set; }

        public bool IsEditor { get; set; } = false;

        public async Task OnGetAsync(Guid id)
        {
            Jobs = await _context.Jobs.OrderBy(job => job.JobName).ToListAsync();
            Job = Jobs.First(job => job.JobId == id);

            Steps = await _context.Steps.Where(step => step.JobId == Job.JobId).ToListAsync();

            Dependencies = await _context.Dependencies.Include(d => d.Step).Include(d => d.DependantOnStep)
                .Where(d => d.Step.JobId == id)
                .OrderBy(d => d.Step.StepName)
                .ThenBy(d => d.DependantOnStep.StepName).ToListAsync();

            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (authorized.Succeeded)
            {
                IsEditor = true;
            }
        }

        public async Task<IActionResult> OnPostRenameJob(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            var job = _context.Jobs.Find(id);

            if (job == null)
            {
                return NotFound();
            }

            job.JobName = NewJobName;

            _context.Attach(job).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Dependencies", new { id });
        }
    }
}
