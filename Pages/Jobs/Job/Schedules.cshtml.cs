using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs
{
    public class SchedulesModel : PageModel
    {

        private readonly EtlManagerContext _context;
        public readonly string WebRootPath;
        private readonly HttpContext httpContext;

        public SchedulesModel(EtlManagerContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            WebRootPath = webHostEnvironment.WebRootPath;
            httpContext = httpContextAccessor.HttpContext;
        }

        public Job Job { get; set; }
        public IList<Schedule> Schedules { get; set; }

        public bool Subscribed { get; set; }

        [BindProperty]
        public Schedule NewSchedule { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Schedules).Include(job => job.Subscriptions).FirstOrDefaultAsync(job => job.JobId == id);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();

            NewSchedule = new Schedule { JobId = id, IsEnabled = true };

            string user = httpContext.User?.Identity?.Name;
            if (Job.Subscriptions.Select(subscription => subscription.Username).Contains(user))
            {
                Subscribed = true;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid? id, Guid jobId)
        {
            if (id == null) return NotFound();

            Schedule schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            
            Job = await _context.Jobs.Include(job => job.Schedules).FirstOrDefaultAsync(job => job.JobId == schedule.JobId);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();
            return RedirectToPage("./Schedules", new { id = jobId });
        }

        public async Task<IActionResult> OnPost()
        {

            _context.Schedules.Add(NewSchedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Schedules", new { id = NewSchedule.JobId });
        }

        public async Task<IActionResult> OnPostToggleSubscribed(Guid id)
        {
            string username = httpContext.User?.Identity?.Name;

            var subscription = await _context.Subscriptions
                .Where(subscription => subscription.JobId == id && subscription.Username == username)
                .FirstOrDefaultAsync();

            try
            {
                if (subscription != null)
                {
                    _context.Subscriptions.Remove(subscription);
                }
                else
                {
                    _context.Subscriptions.Add(new Subscription { JobId = id, Username = username });
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error toggling subscription: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }
    }
}
