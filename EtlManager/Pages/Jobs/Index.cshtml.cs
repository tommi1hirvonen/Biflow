using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace EtlManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpContext httpContext;
        private readonly IAuthorizationService _authorizationService;

        public IndexModel(IConfiguration configuration, EtlManagerContext context, IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService)
        {
            _context = context;
            _configuration = configuration;
            httpContext = httpContextAccessor.HttpContext;
            _authorizationService = authorizationService;
        }

        public IList<Job> Jobs { get;set; }

        public Job NewJob { get; set; }

        public Dictionary<Guid, JobExecution> LastExecutions { get; set; } = new Dictionary<Guid, JobExecution>();

        public Job EditJob { get; set; }

        public bool IsEditor { get; set; } = false;

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.Include(job => job.Steps).Include(job => job.Schedules).OrderBy(job => job.JobName).ToListAsync();
            
            var lastExecutions = await _context.JobExecutions
                .Where(execution => Jobs.Select(job => job.JobId).Contains(execution.JobId) && execution.StartDateTime != null)
                .Select(execution => execution.JobId)
                .Distinct()
                .Select(key => new
                {
                    Key = key,
                    Execution = _context.JobExecutions.Where(execution => execution.JobId == key).OrderByDescending(e => e.CreatedDateTime).First()
                })
                .ToListAsync();

            lastExecutions.ForEach(item => LastExecutions[item.Key] = item.Execution);

            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (authorized.Succeeded)
            {
                IsEditor = true;
            }
        }

        public async Task<IActionResult> OnPostCopy(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            string user = httpContext.User?.Identity?.Name;

            await Utility.JobCopy(_configuration, id, user);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (id == null) return NotFound();

            Job job= await _context.Jobs.FindAsync(id);

            if (job == null) return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCreate([Bind("JobId", "JobName", "CreatedDateTime", "LastModifiedDateTime", "UseDependencyMode")] Job NewJob)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }
            _context.Jobs.Add(NewJob);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostEdit([Bind("JobId", "JobName", "CreatedDateTime", "LastModifiedDateTime", "UseDependencyMode")] Job EditJob)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(EditJob).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(EditJob.JobId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostToggleJobEnabled(Guid? id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            Job job = await _context.Jobs.FindAsync(id);

            if (job == null)
            {
                return new JsonResult(new { success = false, responseText = "No job found for id " + id });
            }

            await Utility.ToggleJobEnabled(_configuration, job);

            return new JsonResult(new { success = true });
        }

        private bool JobExists(Guid id)
        {
            return _context.Jobs.Any(e => e.JobId == id);
        }

    }
}
