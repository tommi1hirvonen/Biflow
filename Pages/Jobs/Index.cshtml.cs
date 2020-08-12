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

namespace EtlManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpContext httpContext;

        public IndexModel(IConfiguration configuration, EtlManagerContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            httpContext = httpContextAccessor.HttpContext;
        }

        public IList<Job> Jobs { get;set; }

        public Job NewJob { get; set; }

        public Job EditJob { get; set; }

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.ToListAsync();
        }

        public async Task<IActionResult> OnPostCopy(Guid id)
        {
            string user = httpContext.User?.Identity?.Name;

            await Utility.JobCopy(_configuration, id, user);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == null) return NotFound();

            Job job= await _context.Jobs.FindAsync(id);

            if (job == null) return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostCreate([Bind("JobId", "JobName", "CreatedDateTime", "LastModifiedDateTime", "UseDependencyMode")] Job NewJob)
        {
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

        private bool JobExists(Guid id)
        {
            return _context.Jobs.Any(e => e.JobId == id);
        }

    }
}
